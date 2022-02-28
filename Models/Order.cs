using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Order_API.Models
{
    public class Order
    {
        [Key]
        public int Cart_ID { get; set; }

        public string Display_ID { get; set; }

        public string Creator { get; set; }

        public DateTime? Creation_Time { get; set; }

        public DateTime? Edit_Time { get; set; }

        public string Product_IDs { get; set; }

        public string Product_Prices { get; set; }

        public DateTime? Order_Time { get; set; }

        public double? Total { get; set; }

        public string Order_Status { get; set; }

        public int? Order_ID { get; set; }

    }
}
