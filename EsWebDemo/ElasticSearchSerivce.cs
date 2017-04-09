using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using EsWebDemo.Models;
using Nest;
using SearchInput = EsWebDemo.Models.SearchInput;

namespace EsWebDemo
{
    public class ElasticSearchSerivce<T> where T : class
    {
        private readonly ElasticClient _elasticClient;

        private readonly string _indexName;

        public ElasticSearchSerivce(string indexName)
        {
            _indexName = indexName;
            _elasticClient = new ElasticClient(new Uri("http://elastic:changeme@localhost:9200"));
        }

        /// <summary>
        /// 创建索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public async Task CreateIndexAsync()
        {
            var ex = await _elasticClient.IndexExistsAsync(_indexName);
            if (!ex.Exists)
            {
                var response = await _elasticClient.CreateIndexAsync(_indexName,
                    c =>
                        c.Index(_indexName)
                        .Settings(
                            s => s.Analysis(an=>an.TokenFilters(filter=>filter.Synonym("syno",syno=>syno.SynonymsPath("analysis/synonym.txt"))).Analyzers(ana=>ana.Custom("ik_syno",ik=>ik.Tokenizer("ik_max_word").Filters("syno")))).NumberOfShards(1).NumberOfReplicas(1).Setting("max_result_window", int.MaxValue))
                        .Mappings(ms => ms.Map<T>(m => m.AutoMap())));
                if (!response.Acknowledged)
                {
                    throw new Exception("创建失败" + response.ServerError.Error.Reason);
                }
            }

        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public async Task AddToEs(List<T> models)
        {
            var bulk = new BulkRequest(_indexName)
            {
                Operations = new List<IBulkOperation>()
            };

            foreach (var model in models)
            {
                bulk.Operations.Add(new BulkIndexOperation<T>(model));
            }

            var response = await _elasticClient.BulkAsync(bulk);
            if (response.Errors)
            {
                throw new Exception($"添加失败{response.Items.First().Error}");
            }

        }

        /// <summary>
        /// 添加到索引中
        /// </summary>
        public async Task AddToEs(T t)
        {
            var response = await _elasticClient.IndexAsync(t, i => i.Index(_indexName));


        }


        /// <summary>
        /// 搜索 默认用搜索字段做高亮字段
        /// </summary>
        /// <param name="input"></param>
        /// <param name="highField">高亮字段</param>
        /// <returns></returns>
        public async Task<SearchResult<Article>> Search(SearchInput input, params string[] highField)
        {
            var query = new SearchDescriptor<Article>();
            query.Index(_indexName);
            var fields = new FieldsDescriptor<Article>();
            var highdes = new HighlightDescriptor<Article>();
            highdes.PreTags("<b style=\"color: red;\">").PostTags("</b>");
            var ishigh = highField != null && highField.Length > 0;
            //搜索字段
            var hfs = new List<Func<Nest.HighlightFieldDescriptor<Article>, IHighlightField>>();

            foreach (var item in input.SearchFields)
            {
                fields.Field(item);
                if (!ishigh)
                {
                    hfs.Add(f => f.Field(item));
                }
            }

            var queryDes = new QueryContainerDescriptor<Article>();
           
            queryDes.MultiMatch(mm => mm.Query(input.Filter).Fields(f => fields));
            query.Query(q => queryDes);

            //排序
            query.Sort(x => x.Field("_score", SortOrder.Descending));
            //分页
            query.Skip(input.Skip).Take(input.Size);
            //关键词高亮

            if (ishigh)
            {
                foreach (var s in highField)
                {
                    hfs.Add(f => f.Field(s));
                }
            }
            highdes.Fields(hfs.ToArray());
            query.Highlight(h => highdes);
            var response = await _elasticClient.SearchAsync<Article>(query);
            var result = new SearchResult<Article>
            {
                Total = response.Total,
                Took = response.Took,
                List = new List<Article>()
            };
            //组装返回数据
            if (response.Total > 0)
            {
                foreach (var responseHit in response.Hits)
                {
                    result.List.Add(new Article()
                    {
                        Title = responseHit.Highlights == null || !responseHit.Highlights.ContainsKey("title") ? responseHit.Source.Title : responseHit.Highlights["title"].Highlights.FirstOrDefault(),
                        Content = responseHit.Highlights == null || !responseHit.Highlights.ContainsKey("content") ? responseHit.Source.Title : responseHit.Highlights["content"].Highlights.FirstOrDefault(),
                        Descirption = responseHit.Highlights == null || !responseHit.Highlights.ContainsKey("descirption") ? responseHit.Source.Title : responseHit.Highlights["descirption"].Highlights.FirstOrDefault(),
                        DateTime = responseHit.Source.DateTime,
                        Id = responseHit.Source.Id
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// 搜索所有
        /// </summary>
        /// <returns></returns>
        public async Task<SearchResult<Article>> Search(int skip, int size)
        {


            var response = await _elasticClient.SearchAsync<Article>(q => q.Index(_indexName).Skip(skip).Size(size));
            var result = new SearchResult<Article>
            {
                Total = response.Total,
                Took = response.Took,
                List = new List<Article>()
            };

            //组装返回数据
            if (response.Total > 0)
            {
                foreach (var responseHit in response.Hits)
                {
                    result.List.Add(new Article()
                    {
                        Title = responseHit.Source.Title,
                        Content = responseHit.Source.Content,
                        Descirption = responseHit.Source.Descirption,
                        DateTime = responseHit.Source.DateTime,
                        Id = responseHit.Source.Id
                    });
                }
            }
            return result;
        }


        /// <summary>
        /// Profile 搜索建议
        /// </summary>
        public async Task<List<string>> Search(string perfix)
        {
            var result = await _elasticClient.SearchAsync<Article>(q => q.Index(_indexName).Source(dd => dd.Includes(a => a.Field(f => f.KeyWord))).Suggest(su => su.Completion("article-suggest", com => com.Field("keyWord").Prefix(perfix).Size(1))));

            if (result.Suggest?.Count > 0)
            {
                var source = result.Suggest["article-suggest"].First().Options.First().Source;
                var list = new List<string> { result.Suggest["article-suggest"].First().Options.First().Text };

                foreach (var keyword in source.KeyWord)
                {
                    list.Add(keyword.Input);
                }
                return list.Distinct().ToList();
            }

            return null;
        }
    }
}