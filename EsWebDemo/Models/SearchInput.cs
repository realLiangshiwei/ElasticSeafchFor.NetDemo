using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EsWebDemo.Models
{
    public class SearchInput
    {
        public string Filter { get; set; }

        public int Skip { get; set; }

        public int Size { get; set; }

        public List<string> SearchFields { get; set; }
    }
}