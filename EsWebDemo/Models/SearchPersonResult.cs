using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EsWebDemo.Models
{
    public class SearchResult<T>
    {
        /// <summary>
        /// 耗时毫秒
        /// </summary>
        public long Took;

        /// <summary>
        /// 总数
        /// </summary>
        public long Total { get; set; }

        public List<T> List { get; set; }


    }

}