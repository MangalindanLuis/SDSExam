using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace Exam.Models
{
    public class RecyclableItem
    {
        public int Id { get; set; }
        public int RecyclableTypeId { get; set; }
        public string Description { get; set; }
        public decimal Weight { get; set; }
        public decimal ComputedRate { get; set; }
        public List<SelectListItem> RecyclableTypeList { get; set; }
    }
}