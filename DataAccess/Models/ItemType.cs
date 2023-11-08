using System;
using System.Collections.Generic;

namespace DataAccess.Models
{
    public partial class ItemType
    {
        public ItemType()
        {
            Items = new HashSet<Item>();
        }

        public int ItemTypeId { get; set; }
        public string? ItemTypeName { get; set; }

        public virtual ICollection<Item> Items { get; set; }
    }
}
