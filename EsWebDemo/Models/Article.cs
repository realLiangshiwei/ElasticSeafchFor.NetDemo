using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nest;

namespace EsWebDemo.Models
{
    [ElasticsearchType(Name = "Article", IdProperty = "Id")]
    public class Article
    {
        [Text(Index = true)]
        public string Id { get; set; }

        [Text(Analyzer = "ik_syno",SearchAnalyzer = "ik_syno")]
        public string Title { get; set; }

        [Text(Analyzer = "ik_syno",SearchAnalyzer = "ik_syno")]
        public string Descirption { get; set; }

        [Text(Analyzer = "ik_syno",SearchAnalyzer = "ik_syno")]
        public string Content { get; set; }

        [Completion(Analyzer = "keyword", SearchAnalyzer = "keyword", PreserveSeparators = false)]
        public List<Keyword> KeyWord { get; set; }

        [Date]
        public DateTime DateTime { get; set; }
    }

    public class Keyword
    {
        public string Input { get; set; }

        public int Weight { get; set; }
    }
}