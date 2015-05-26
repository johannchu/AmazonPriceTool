using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GetAmazonPriceTool
{
    class Product
    {
        public string itemName;
        public string itemPriceWhenAdded;
        public string itemWebpageURL;
        public string localItemImagePath;
        public string timeOfItemAdded;
        public string dateOfItemAdded;

        public Product()
        {
            itemName = "";
            itemName = "";
            itemWebpageURL = "";
            localItemImagePath = "";
        }

        public Product(string itemWebpageURL, string localItemImagePath, string itemName, string itemPriceWhenAdded, string dateOfItemAdded, string timeOfItemAdded)
        {
            this.itemName = itemName;
            this.itemPriceWhenAdded = itemPriceWhenAdded;
            this.itemWebpageURL = itemWebpageURL;
            this.localItemImagePath = localItemImagePath;
            this.dateOfItemAdded = dateOfItemAdded;
            this.timeOfItemAdded = timeOfItemAdded;
        }

    }
}
