using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Media.Imaging;
using System.Reflection;

namespace GetAmazonPriceTool
{
    public partial class Form1 : Form
    {
        int numberOfMerchandiseDesired = 0;
        int indexOfCurrentRowSelectedByUser;
        string appPath = Path.GetDirectoryName(Application.ExecutablePath);
        string directoryForOriginalSourceFileOfWebpage = "";
        string itemWebPageURL = "";
        string itemImageURL = "";
        string itemName = "";
        string itemPrice = "";        
        // List<string> addTimeItemPrice = new List<string>();
        List<string> latestItemPrice = new List<string>();
        string dateOfItemUpdate = "";
        List<string> latestDateOfItemUpdate = new List<string>();
        string timeOfItemUpdate = "";
        List<string> latestTimeOfItemUpdate = new List<string>();
        FileStream fileStreamToOpenTextFile;

        StreamReader sr = null;
        StreamWriter sw = null;
        WebClient downloadItemImage = new WebClient();
        List<string> dataFromIndex;  // Read from index text file and store here.
        List<Product> productList;
        List<string> indexSegment;   // SPLIT what you have received above and store here.
        List<string> bufferForReadingTempFile;
        System.Timers.Timer timerForPeriodicUpdate = new System.Timers.Timer();
        
        public Form1()
        {
            InitializeComponent();
            comboBox1.Enabled = false;
            comboBox2.Enabled = false;
            directoryForOriginalSourceFileOfWebpage = appPath + "\\temp.txt";
            if (!Directory.Exists(appPath+"\\Temp"))
            {
                Directory.CreateDirectory(appPath + "\\Temp");
            }
            if (!File.Exists(appPath+"\\Temp\\Index.txt"))
            {
                //FileStream fs = new FileStream(appPath + "\\Temp\\Index.txt", FileMode.Create, FileAccess.Write);
                // fs.Close();
                sw = new StreamWriter(File.Open(appPath + "\\Temp\\Index.txt", FileMode.Create), Encoding.UTF8);
                sw.Close();
            }
            if (!File.Exists(appPath + "\\Temp\\LatestPriceDateTime.txt"))
            {
                FileStream fs = new FileStream(appPath + "\\Temp\\LatestPriceDateTime.txt", FileMode.Create, FileAccess.Write);
                fs.Close();
            }
            DataGridViewCellStyle dataGridViewCellStyle = new DataGridViewCellStyle();
            dataGridViewCellStyle.NullValue = null;
            dataGridViewCellStyle.Tag = "blank";
            readFromLastSavedIndex(appPath + "\\Temp\\Index.txt");
            readFromLastSavedPriceDateTime(appPath + "\\Temp\\LatestPriceDateTime.txt");
            notifyIcon1.Visible = false;
        }
        
        private void readFromLastSavedPriceDateTime(string localfile)
        { 
            using(fileStreamToOpenTextFile = new FileStream(localfile, FileMode.Open, FileAccess.Read))
            {
                sr = new StreamReader(fileStreamToOpenTextFile);
                List<string> dataFromLatestPriceDateTime = new List<string>();
                while (true)
                {
                    string input = sr.ReadLine();
                    if (input == null | input == "")
                    {
                        break;
                    }
                    dataFromLatestPriceDateTime.Add(input);
                }
                for (int i = 0; i <= dataFromLatestPriceDateTime.Count - 1; i++)
                {
                    indexSegment = new List<string>(dataFromLatestPriceDateTime[i].Split(','));
                    latestItemPrice.Add(indexSegment[0]);
                    latestDateOfItemUpdate.Add(indexSegment[1]);
                    latestTimeOfItemUpdate.Add(indexSegment[2]);
                    dataGridView1.Rows[i].Cells[2].Value= "$ "+indexSegment[0] + "\n\nLast updated:\n" + indexSegment[1] +"\n"+ indexSegment[2];
                }
            }
        }
        
        private void readFromLastSavedIndex(string lastSavedLogFile)
        {
            using (fileStreamToOpenTextFile=new FileStream(lastSavedLogFile, FileMode.Open, FileAccess.Read))
            {
                sr = new StreamReader(fileStreamToOpenTextFile, Encoding.UTF8);
                dataFromIndex = new List<string>();
                productList = new List<Product>();
                while (true)
                {
                    string input = sr.ReadLine();
                    if (input == null | input == "")
                    {
                        break;
                    }
                    dataFromIndex.Add(input);
                }
                // MessageBox.Show("dataFromIndex: " + dataFromIndex.Count, "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                for (int i = 0; i <= dataFromIndex.Count - 1; i++)
                {
                    numberOfMerchandiseDesired++;
                    dataGridView1.Rows.Add(1);
                    indexSegment = new List<string>(dataFromIndex[i].Split(','));
                    // addTimeItemPrice.Add(indexSegment[3]);
                    productList.Add(new Product(indexSegment[0], indexSegment[1], indexSegment[2], indexSegment[3], indexSegment[4], indexSegment[5]));
                    showItemImageNamePrice(indexSegment[1], indexSegment[2], indexSegment[3], indexSegment[3], indexSegment[4], indexSegment[5]);
                }
                MessageBox.Show("Current items in your wait list: " + numberOfMerchandiseDesired + "\nHave a great day!",
                    "Greetings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }            
        }
             
        private void button1_Click(object sender, EventArgs e)
        {
            itemImageURL = "";
            itemName = "";
            itemPrice = "";
            if (textBox1.Text!="")
            {
                itemWebPageURL = textBox1.Text;
                try
                {
                    DownloadWebFileAsTemp(itemWebPageURL);
                    dataGridView1.BackgroundColor = Color.LightGray;
                }
                catch
                {
                    textBox1.Text = "";
                    dataGridView1.BackgroundColor = Color.White;
                    MessageBox.Show("Error in the URL. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }                        
            if (itemName != "" & itemPrice != "")
            {                
                numberOfMerchandiseDesired++;
                dataGridView1.Rows.Add(1);
                saveItemDetailIntoTextAndImage(itemWebPageURL, itemImageURL, itemName, itemPrice, dateOfItemUpdate, timeOfItemUpdate);
                // MessageBox.Show("Current row length: " + dataGridView1.Rows.Count, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (itemImageURL != "")
                {
                    showItemImageNamePrice(appPath + "\\Temp\\item_" + numberOfMerchandiseDesired + ".jpg", itemName, itemPrice, itemPrice, dateOfItemUpdate, timeOfItemUpdate);
                }
                else
                {
                    showItemImageNamePrice("", itemName, itemPrice, itemPrice, dateOfItemUpdate, timeOfItemUpdate);
                }
            }
            else
            {
                MessageBox.Show("Unable to extract information. Sorry!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            dataGridView1.BackgroundColor = Color.White;
            textBox1.Text = "";
             MessageBox.Show("The page has been extracted.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DownloadWebFileAsTemp(string remoteFile)
        {
            string localFile = directoryForOriginalSourceFileOfWebpage;
            // sw = new StreamWriter(File.Open(localFile, FileMode.Create), Encoding.UTF8);
            // sw.Close();
            FileStream localFileStream = new FileStream(localFile, FileMode.Create, FileAccess.ReadWrite);
            
            WebRequest webRequest = WebRequest.Create(remoteFile);
            webRequest.Method = WebRequestMethods.Http.Get;
            //Stream webResponseStream;
            //webRequest.BeginGetResponse(delegate (IAsyncResult asyncResult)
            //{
            //    using (WebResponse webResponse = (HttpWebResponse)webRequest.EndGetResponse(asyncResult))
            //    using (webResponseStream = webResponse.GetResponseStream())
            //    using (StreamReader reader = new StreamReader(webResponseStream))
            //    {
            //        byte[] buffer = new byte[1024];  // Initializing a "byte" array with 1024 spaces.
            //        int bytesRead = webResponseStream.Read(buffer, 0, 1024);
            //        while (bytesRead > 0)
            //        {
            //            localFileStream.Write(buffer, 0, bytesRead);
            //            bytesRead = webResponseStream.Read(buffer, 0, 1024);
            //        }
            //    }
            //}, null);
            WebResponse webResponse = webRequest.GetResponse();
            Stream webResponseStream = webResponse.GetResponseStream();

            byte[] buffer = new byte[1024];  // Initializing a "byte" array with 1024 spaces.
            int bytesRead = webResponseStream.Read(buffer, 0, 1024);
            if (remoteFile.Contains("amazon") | remoteFile.Contains("yahoo"))
            {
                while (bytesRead > 0)
                {
                    localFileStream.Write(buffer, 0, bytesRead);
                    bytesRead = webResponseStream.Read(buffer, 0, 1024);
                }
            }
            else if (remoteFile.Contains("pchome"))
            {
                while (bytesRead > 0)
                {
                    localFileStream.Write(Encoding.Convert(Encoding.GetEncoding(950), Encoding.UTF8, buffer), 0, bytesRead);
                    bytesRead = webResponseStream.Read(buffer, 0, 1024);
                }
            }            
            localFileStream.Close();
            webResponseStream.Close();

            // Now, throw the temp file into the fitted function below by identifying the URL.
            if (remoteFile.Contains("amazon") == true)
            {
                try
                {
                    AnalyzeAmazonItemDetail(localFile);
                }
                catch
                {
                    MessageBox.Show("Error occurred during data analysis. Sorry!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (remoteFile.Contains("pchome") == true)
            {
                try
                {
                    AnalyzePchomeItemDetail(localFile);
                }
                catch
                {
                    MessageBox.Show("Error occurred during data analysis. Sorry!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (remoteFile.Contains("yahoo") == true)
            {
                try
                {
                    AnalyzeYahooItemDetail(localFile);
                }
                catch
                {
                    MessageBox.Show("Error occurred during data analysis. Sorry!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            dateOfItemUpdate = DateTime.Now.ToString("yyyy/MM/dd");
            timeOfItemUpdate = DateTime.Now.ToString("HH:mm:ss");            
        }

        private void AnalyzeAmazonItemDetail(string localFile)
        {
            string patternForItemImageURL = @"(?<=<img id=""main-image"" src="").*(?="" alt)";
            string patternForItemImageURL2 = @"(?<=var i=new Image;i.src = "").*(?="")";
            // string patternForItemName = @"(?<=<meta name=""title"" content=""Amazon.com: ).*(?=:)";
            string patternForItemName = @"(?<=<span id=""btAsinTitle""\s*>).*(?=<)";
            string patternForItemPrice = @"(?<=""itemData"":\D*\d?\D*\s*buyingPrice"":)(\d+\.?\d+)(?=,""ASIN"")";
            string patternForItemPrice2 = @"((?<=<td id=""actualPriceContent""><span id=""actualPriceValue""><b class=""priceLarge"">\$).*(?=</b>))";

            bufferForReadingTempFile = new List<string>();
            itemImageURL = "";
            itemName = "";
            itemPrice = "";

            Match matchForImageURL;
            Match matchForImageURL2;
            Match matchForItemName;
            Match matchForItemPrice;
            Match matchForItemPrice2;

            Regex getItemImageURL = new Regex(patternForItemImageURL, RegexOptions.Singleline);
            Regex getItemImageURL2 = new Regex(patternForItemImageURL2, RegexOptions.Singleline);
            Regex getItemName = new Regex(patternForItemName, RegexOptions.Singleline);
            Regex getItemPrice = new Regex(patternForItemPrice, RegexOptions.Singleline);
            Regex getItemPrice2 = new Regex(patternForItemPrice2, RegexOptions.Singleline);

            using (fileStreamToOpenTextFile = new FileStream(localFile, FileMode.Open, FileAccess.Read))
            {
                sr = new StreamReader(fileStreamToOpenTextFile);
                while (!sr.EndOfStream)
                {
                    string input = sr.ReadLine();
                    bufferForReadingTempFile.Add(input);
                }
                // MessageBox.Show("Buffer for Reading Temp Size: " + bufferForReadingTempFile.Length, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                for (int i = 0; i <= bufferForReadingTempFile.Count - 1; i++)
                { 
                    matchForImageURL = getItemImageURL.Match(bufferForReadingTempFile[i]);
                    matchForImageURL2 = getItemImageURL2.Match(bufferForReadingTempFile[i]);
                    matchForItemName = getItemName.Match(bufferForReadingTempFile[i]);
                    matchForItemPrice = getItemPrice.Match(bufferForReadingTempFile[i]);
                    matchForItemPrice2 = getItemPrice2.Match(bufferForReadingTempFile[i]);

                    if (matchForImageURL.Success & itemImageURL=="")
                    {
                        itemImageURL = matchForImageURL.ToString();
                    }
                    if (matchForImageURL2.Success & itemImageURL=="")
                    {
                        itemImageURL = matchForImageURL2.ToString();
                    }
                    if (matchForItemName.Success)
                    {
                        string tempName = matchForItemName.ToString();
                        itemName = tempName.Replace(",","");
                    }
                    if (matchForItemPrice.Success & itemPrice=="")
                    {
                        string tempPrice = matchForItemPrice.ToString();
                        itemPrice = tempPrice.Replace(",","");                                              
                    }
                    if (matchForItemPrice2.Success & itemPrice=="")
                    {
                        string tempPrice = matchForItemPrice2.ToString();
                        itemPrice = tempPrice.Replace(",", "");
                    }
                    if (!itemImageURL.Equals("") & !itemName.Equals("") & !itemPrice.Equals(""))
                    {
                        break;
                    }                    
                }                
            }
            // MessageBox.Show("Item Name: "+itemName+"\nItem Image URL: "+itemImageURL+"\nItem Price: "+itemPrice, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AnalyzePchomeItemDetail(string localFile)
        {
            string patternForItemImageURL = @"(?<=<span id='spec_pic'><img src="").*(?=""\s*alt=)";
            string patternForItemName = @"(?<=<input type=""hidden"" id=""IT_NAME""\s*name=""IT_NAME""\s*value="").*(?="">)";
            string patternForItemPrice = @"(?<=<input type=""hidden"" id=""IT_PRICE""\s*name=""IT_PRICE""\s*value="").*(?="">)";
            
            bufferForReadingTempFile = new List<string>();
            itemImageURL = "";
            itemName = "";
            itemPrice = "";

            Match matchForImageURL;
            Match matchForItemName;
            Match matchForItemPrice;
            
            Regex getItemImageURL = new Regex(patternForItemImageURL, RegexOptions.Singleline);
            Regex getItemName = new Regex(patternForItemName, RegexOptions.Singleline);
            Regex getItemPrice = new Regex(patternForItemPrice, RegexOptions.Singleline);
            
            using (fileStreamToOpenTextFile = new FileStream(localFile, FileMode.Open, FileAccess.Read))
            {
                sr = new StreamReader(fileStreamToOpenTextFile);
                while (!sr.EndOfStream)
                {
                    string input = sr.ReadLine();
                    bufferForReadingTempFile.Add(input);
                }
                // MessageBox.Show("Buffer for Reading Temp Size: " + bufferForReadingTempFile.Length, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                for (int i = 0; i <= bufferForReadingTempFile.Count - 1; i++)
                {
                    matchForImageURL = getItemImageURL.Match(bufferForReadingTempFile[i]);
                    // matchForImageURL2 = getItemImageURL2.Match(bufferForReadingTempFile[i]);
                    matchForItemName = getItemName.Match(bufferForReadingTempFile[i]);
                    matchForItemPrice = getItemPrice.Match(bufferForReadingTempFile[i]);
                    // matchForItemPrice2 = getItemPrice2.Match(bufferForReadingTempFile[i]);

                    if (matchForImageURL.Success & itemImageURL == "")
                    {
                        itemImageURL = matchForImageURL.ToString();
                        if (!itemImageURL.StartsWith("//ec1img.pchome.com.tw"))
                        {
                            itemImageURL = "http://ec1img.pchome.com.tw/pic/v1/" + itemImageURL;
                        }
                        else
                        {
                            itemImageURL = "http:" + itemImageURL;
                        }
                    }
                    //if (matchForImageURL2.Success & itemImageURL == "")
                    //{
                    //    itemImageURL = matchForImageURL2.ToString();
                    //}
                    if (matchForItemName.Success)
                    {
                        string tempName = matchForItemName.ToString();
                        tempName = Encoding.UTF8.GetString(Encoding.Convert(Encoding.GetEncoding(950), Encoding.UTF8, Encoding.GetEncoding(950).GetBytes(tempName)));
                        itemName = tempName.Replace(",", "");
                    }
                    if (matchForItemPrice.Success & itemPrice == "")
                    {
                        string tempPrice = matchForItemPrice.ToString();
                        itemPrice = tempPrice.Replace(",", "");
                    }
                    //if (matchForItemPrice2.Success & itemPrice == "")
                    //{
                    //    string tempPrice = matchForItemPrice2.ToString();
                    //    itemPrice = tempPrice.Replace(",", "");
                    //}
                    if (!itemImageURL.Equals("") & !itemName.Equals("") & !itemPrice.Equals(""))
                    {
                        break;
                    }
                }
            }
        }

        private void AnalyzeYahooItemDetail(string localFile)
        {
            string patternForItemImageURL1stLine = @"<div class=""Prod_img"">";
            string patternForItemImageURL2ndLind = @"(?<=img src="").*(?="" alt)";
            string patternForItemName = @"(?<=<!--prodnm="").*(?=""-->)";
            string patternForItemPrice = @"(?<=<input type=""hidden"" id=""gdprice"" value="").*(?="">)";
            
            bufferForReadingTempFile = new List<string>();
            itemImageURL = "";
            itemName = "";
            itemPrice = "";

            Match matchForItemImageURL1stLine;
            Match matchForItemImageURL2ndLine;
            Match matchForItemName;
            Match matchForItemPrice;
            
            Regex getItemImageURL1stLine = new Regex(patternForItemImageURL1stLine, RegexOptions.Singleline);
            Regex getItemImageURL2ndLine = new Regex(patternForItemImageURL2ndLind, RegexOptions.Singleline);
            Regex getItemName = new Regex(patternForItemName, RegexOptions.Singleline);
            Regex getItemPrice = new Regex(patternForItemPrice, RegexOptions.Singleline);
            
            using (fileStreamToOpenTextFile = new FileStream(localFile, FileMode.Open, FileAccess.Read))
            {
                sr = new StreamReader(fileStreamToOpenTextFile);
                while (!sr.EndOfStream)
                {
                    string input = sr.ReadLine();
                    bufferForReadingTempFile.Add(input);
                }
                // MessageBox.Show("Buffer for Reading Temp Size: " + bufferForReadingTempFile.Length, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                for (int i = 0; i <= bufferForReadingTempFile.Count - 1; i++)
                {
                    matchForItemImageURL1stLine = getItemImageURL1stLine.Match(bufferForReadingTempFile[i]);
                    matchForItemName = getItemName.Match(bufferForReadingTempFile[i]);
                    matchForItemPrice = getItemPrice.Match(bufferForReadingTempFile[i]);

                    if (matchForItemImageURL1stLine.Success)
                    {
                        matchForItemImageURL2ndLine = getItemImageURL2ndLine.Match(bufferForReadingTempFile[i + 1]);
                        if (matchForItemImageURL2ndLine.Success)
                        {
                            itemImageURL = matchForItemImageURL2ndLine.ToString();
                            // MessageBox.Show("Item imageURL found.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    
                    if (matchForItemName.Success)
                    {
                        string tempName = matchForItemName.ToString();
                        itemName = tempName.Replace(",", "");
                        // MessageBox.Show("Item name found.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    if (matchForItemPrice.Success & itemPrice == "")
                    {
                        string tempPrice = matchForItemPrice.ToString();
                        itemPrice = tempPrice.Replace(",", "");
                        // MessageBox.Show("Item price found.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    
                    if (!itemImageURL.Equals("") & !itemName.Equals("") & !itemPrice.Equals(""))
                    {
                        break;
                    }
                }
            }
            // MessageBox.Show("Item Name: " + itemName + "\nItem Image URL: " + itemImageURL + "\nItem Price: " + itemPrice, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void saveItemDetailIntoTextAndImage(string inputItemWebPageURL, string inputItemImageURL, string inputItemName, string inputItemPrice, string inputDateOfItemUpdate, string inputTimeOfItemUpdate)
        {
            fileStreamToOpenTextFile = File.Open(appPath + "\\Temp\\Index.txt", FileMode.Append, FileAccess.Write);
            string localItemImagePath = appPath + "\\Temp\\item_" + numberOfMerchandiseDesired + ".jpg";
            using (sw = new StreamWriter(fileStreamToOpenTextFile))
            {
                if (inputItemImageURL != "")
                {
                    if (inputItemImageURL.StartsWith("http"))
                    {
                        try
                        {
                            downloadItemImage.DownloadFile(inputItemImageURL, localItemImagePath);
                            sw.Write(inputItemWebPageURL + "," + localItemImagePath + "," + inputItemName + "," + inputItemPrice + "," + inputDateOfItemUpdate + "," + inputTimeOfItemUpdate + "\r\n");
                        }
                        catch
                        {
                            MessageBox.Show("Error occurred during data saving. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        try
                        {
                            downloadItemImage.DownloadFile("http:" + inputItemImageURL, localItemImagePath);
                            sw.Write(inputItemWebPageURL + "," + localItemImagePath + "," + inputItemName + "," + inputItemPrice + "," + inputDateOfItemUpdate + "," + inputTimeOfItemUpdate + "\r\n");
                        }
                        catch
                        {
                            MessageBox.Show("Error occurred during data saving. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                else
                {
                    sw.Write(inputItemWebPageURL + ",," + inputItemName + "," + inputItemPrice + "," + inputDateOfItemUpdate + "," + inputTimeOfItemUpdate + "\r\n");
                }
            }
            // Create a new update time log file for the new item.
            FileStream fileStreamToCreateItemTextFile = File.Create(appPath + "\\Temp\\Item_" + numberOfMerchandiseDesired + ".txt");
            using (sw = new StreamWriter(fileStreamToCreateItemTextFile))
            {
                sw.Write(inputItemPrice+","+inputDateOfItemUpdate+","+inputTimeOfItemUpdate);
            }
            productList.Add(new Product(inputItemWebPageURL, localItemImagePath, inputItemName, inputItemPrice, inputDateOfItemUpdate, inputTimeOfItemUpdate));
            latestItemPrice.Add(inputItemPrice);
            
            latestDateOfItemUpdate.Add(inputDateOfItemUpdate);
            latestTimeOfItemUpdate.Add(inputTimeOfItemUpdate);
        }

        public bool ThumbnailCallback()// Required for generating thumbnail item image.
        {
            return false;  
        }

        private void showItemImageNamePrice(string localItemImagePath, string localItemName, string localItemPriceOldest, string localItemPriceLatest, string localDateOfItemUpdate, string localTimeOfItemUpdate)
        {
            if (localItemImagePath!="")
            {
                byte[] bufferToLoadImage = File.ReadAllBytes(localItemImagePath);
                MemoryStream memoryStream = new MemoryStream(bufferToLoadImage);
                // Image imgToAdd = (Bitmap)Image.FromStream(memoryStream);
                
                dataGridView1.Rows[numberOfMerchandiseDesired - 1].Height = 80;
                Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);
                Bitmap myBitmap = new Bitmap(memoryStream);
                double imageRatio = Convert.ToDouble(myBitmap.Width) / Convert.ToDouble(myBitmap.Height);
                // MessageBox.Show("imageRatio: " + imageRatio, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                double newRowHeight = dataGridView1.Columns[0].Width / imageRatio;
                if (newRowHeight > 80)
                {
                    dataGridView1.Rows[numberOfMerchandiseDesired - 1].Height = Convert.ToInt16(newRowHeight);
                }
                Image imgToAdd = myBitmap.GetThumbnailImage(dataGridView1.Columns[0].Width,
                    Convert.ToInt16(dataGridView1.Columns[0].Width / imageRatio), myCallback, IntPtr.Zero);
                dataGridView1.Rows[numberOfMerchandiseDesired - 1].Cells[0].Value = imgToAdd;
            }
            else
            {
                dataGridView1.Rows[numberOfMerchandiseDesired - 1].Height = 80;
            }
            dataGridView1.Rows[numberOfMerchandiseDesired - 1].Cells[1].Value = localItemName + "\n(Price when added: $" + localItemPriceOldest + ")";
            dataGridView1.Rows[numberOfMerchandiseDesired - 1].Cells[2].Value = "$ " + localItemPriceLatest + "\n\nLast updated:\n" + localDateOfItemUpdate + "\n" + localTimeOfItemUpdate;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            updateAllExistingItemPrice();
            if (checkBox1.Checked == true & numberOfMerchandiseDesired!=0)
            {
                getHoursAndMinutesAndStartNewTimer();
            }

            // MessageBox.Show("All prices have been updated.\nDate: " + DateTime.Now.ToString("yyyy/MM/dd") + "\nTime: " + DateTime.Now.ToString("HH:mm:ss tt"), 
            //    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void updateAllExistingItemPrice()
        {
            if (numberOfMerchandiseDesired != 0)
            {
                dataGridView1.AllowUserToAddRows = false;
                dataGridView1.AllowUserToDeleteRows = false;

                for (int i = 0; i <= productList.Count - 1; i++)
                {
                    itemPrice = "";
                    DownloadWebFileAsTemp(productList[i].itemWebpageURL); // Grab the webpage URL.
                    using (sw = new StreamWriter(appPath + "\\Temp\\item_" + (i + 1) + ".txt", true))
                    {
                        if (String.Equals(itemPrice, latestItemPrice[i]) == false)
                        {
                            sw.Write("\r\n" + itemPrice + "," + dateOfItemUpdate + "," + timeOfItemUpdate);
                            MessageBox.Show("Recent price change!\nItem Name: " + productList[i].itemName + "\nOld price: "
                                + latestItemPrice[i] + "\nNew price: " + itemPrice, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (String.Equals(itemPrice, latestItemPrice[i]) == true & String.Equals(dateOfItemUpdate, latestDateOfItemUpdate[i]) == true)
                        {
                            sw.Write("," + timeOfItemUpdate);
                        }
                        else if (String.Equals(itemPrice, latestItemPrice[i]) == true & String.Equals(dateOfItemUpdate, latestDateOfItemUpdate[i]) == false)
                        {
                            sw.Write("\r\n" + itemPrice + "," + dateOfItemUpdate + "," + timeOfItemUpdate);
                        }
                        latestItemPrice[i] = itemPrice;
                        latestDateOfItemUpdate[i] = dateOfItemUpdate;
                        latestTimeOfItemUpdate[i] = timeOfItemUpdate;
                    }
                    dataGridView1.Rows[i].Cells[2].Style.ForeColor = Color.Green;
                    dataGridView1.Rows[i].Cells[2].Value = "$ " + latestItemPrice[i] + "\n\nLast updated:\n" + latestDateOfItemUpdate[i] + "\n" + latestTimeOfItemUpdate[i];
                }
                dataGridView1.AllowUserToAddRows = true;
                dataGridView1.AllowUserToDeleteRows = true;
            }
        }        

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                getHoursAndMinutesAndStartNewTimer();
            }
            else
            { 
                comboBox1.Enabled = false;
                comboBox2.Enabled = false;
                timerForPeriodicUpdate.Stop();
            }                
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // MessageBox.Show("OnTimedEvent Activated.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            updateAllExistingItemPrice();
        }

        private void getHoursAndMinutesAndStartNewTimer()
        {
            timerForPeriodicUpdate = new System.Timers.Timer();
            timerForPeriodicUpdate.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            if (comboBox1.Text != "" | comboBox2.Text != "")
            {
                if (comboBox1.Text == "")
                {
                    timerForPeriodicUpdate.Interval = Convert.ToInt16(comboBox2.Text) * 60 * 1000;
                    timerForPeriodicUpdate.Start();
                }
                else if (comboBox2.Text == "")
                {
                    timerForPeriodicUpdate.Interval = Convert.ToInt16(comboBox1.Text) * 60 * 60 * 1000;
                    timerForPeriodicUpdate.Start();
                }
                else
                {
                    timerForPeriodicUpdate.Interval =
                        Convert.ToInt16(comboBox1.Text) * 3600000 + Convert.ToInt16(comboBox2.Text) * 60000;
                    timerForPeriodicUpdate.Start();
                }
                GC.KeepAlive(timerForPeriodicUpdate);
                // MessageBox.Show("Timer is set.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            getHoursAndMinutesAndStartNewTimer();
            // MessageBox.Show("comboBox1 Selected Index Changed.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            getHoursAndMinutesAndStartNewTimer();
            // MessageBox.Show("comboBox2 Selected Index Changed.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        
        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            dataGridView1.Enabled = false;
            dataGridView1.BackgroundColor = Color.LightGray;
            dataGridView1.AllowUserToAddRows = false;
            indexOfCurrentRowSelectedByUser = e.Row.Index;
            
            string[] tempLatestItemPrice = latestItemPrice.ToArray();
            string[] tempLatestDateOfItemUpdate = latestDateOfItemUpdate.ToArray();
            string[] tempLatestTimeOfItemUpdate = latestTimeOfItemUpdate.ToArray();
            List<Product> tempProductList = new List<Product>();
            latestItemPrice = new List<string>();
            latestDateOfItemUpdate = new List<string>();
            latestTimeOfItemUpdate = new List<string>();
            
            // After saving the log file into buffer, create a new blank log file.
            // Notice how, at this part, the fileMode is set to "Create".
            using (sw = File.CreateText(appPath + "\\Temp\\Index_temp.txt"))
            {
                for (int i = 0; i <= indexOfCurrentRowSelectedByUser-1; i++)
                {
                    try
                    {
                        latestItemPrice.Add(tempLatestItemPrice[i]);
                        latestDateOfItemUpdate.Add(tempLatestDateOfItemUpdate[i]);
                        latestTimeOfItemUpdate.Add(tempLatestTimeOfItemUpdate[i]);
                        tempProductList.Add(new Product(productList[i].itemWebpageURL, productList[i].localItemImagePath, productList[i].itemName,
                            productList[i].itemPriceWhenAdded, productList[i].dateOfItemAdded, productList[i].timeOfItemAdded));
                        sw.WriteLine(productList[i].itemWebpageURL + "," + productList[i].localItemImagePath + "," + productList[i].itemName
                            + "," + productList[i].itemPriceWhenAdded + "," + productList[i].dateOfItemAdded + "," + productList[i].timeOfItemAdded);
                        // MessageBox.Show(dataFromIndex[i], "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch
                    { 
                    }
                }
            }
            // Kill the update time log of the selected item.
            if (System.IO.File.Exists(appPath + "\\Temp\\item_" + Convert.ToString(indexOfCurrentRowSelectedByUser + 1) + ".txt"))
            {
                try  
                {
                    System.IO.File.Delete(appPath + "\\Temp\\item_" + Convert.ToString(indexOfCurrentRowSelectedByUser + 1) + ".txt");
                }
                catch
                {
                    MessageBox.Show("Images currently used by other program. Try later!",
                        "Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            // Kill the image of the selected item.
            if (System.IO.File.Exists(appPath + "\\Temp\\item_" + Convert.ToString(indexOfCurrentRowSelectedByUser + 1) + ".jpg"))
            {
                try  
                {
                    System.IO.File.Delete(appPath + "\\Temp\\item_" + Convert.ToString(indexOfCurrentRowSelectedByUser + 1) + ".jpg");
                }
                catch
                {
                    MessageBox.Show("Images currently used by other program. Try later!",
                        "Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }    
            // Change the file name of existing item image.
            for (int j = indexOfCurrentRowSelectedByUser + 1; j <= numberOfMerchandiseDesired - 1; j++)
            {
                if (System.IO.File.Exists(appPath + "\\Temp\\item_" + Convert.ToString(j + 1) + ".jpg"))
                {   
                    File.Move(appPath + "\\Temp\\item_" + Convert.ToString(j + 1) + ".jpg",
                              appPath + "\\Temp\\item_" + Convert.ToString(j) + ".jpg");
                }
                File.Move(appPath + "\\Temp\\item_" + Convert.ToString(j + 1) + ".txt", 
                          appPath + "\\Temp\\item_" + Convert.ToString(j) + ".txt");
                productList[j - 1] = productList[j];
                
                using (sw = new StreamWriter(appPath + "\\Temp\\Index_temp.txt", true))
                {
                    sw.Write(productList[j-1].itemWebpageURL + "," + appPath + "\\Temp\\item_" + Convert.ToString(j) + ".jpg"+","
                        +productList[j-1].itemName+","+productList[j-1].itemPriceWhenAdded+","+productList[j-1].dateOfItemAdded+","+productList[j-1].timeOfItemAdded+"\r\n");
                }
                latestItemPrice.Add(tempLatestItemPrice[j]);
                latestDateOfItemUpdate.Add(tempLatestDateOfItemUpdate[j]);
                latestTimeOfItemUpdate.Add(tempLatestTimeOfItemUpdate[j]);
            }
            productList.RemoveAt(numberOfMerchandiseDesired-1);
            
            if (System.IO.File.Exists(appPath + "\\Temp\\Index.txt"))
            {
                try  // Kill the image of the selected item.
                {
                    System.IO.File.Delete(appPath + "\\Temp\\Index.txt");
                }
                catch
                {
                    MessageBox.Show("Images currently used by other program. Try later!",
                        "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            File.Move(appPath + "\\Temp\\Index_temp.txt", appPath + "\\Temp\\Index.txt");
            numberOfMerchandiseDesired--;
            dataGridView1.AllowUserToAddRows = true;
            dataGridView1.BackgroundColor = Color.White;
            dataGridView1.Enabled = true;
        }
        
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized==this.WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal==this.WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            FileStream fsForWritingLatestPriceDateTime = new FileStream(appPath + "\\Temp\\LatestPriceDateTime.txt", FileMode.Create, FileAccess.Write);
            using (sw = new StreamWriter(fsForWritingLatestPriceDateTime))
            {
                for (int i = 0; i <= latestItemPrice.Count - 1; i++)
                {
                    sw.WriteLine(latestItemPrice[i]+","+latestDateOfItemUpdate[i]+","+latestTimeOfItemUpdate[i]);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show(latestItemPrice[0]+","+latestDateOfItemUpdate[0]+","+latestTimeOfItemUpdate[0],
                "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs anError)
        {
            MessageBox.Show("Error happened " + anError.Context.ToString());

            if (anError.Context == DataGridViewDataErrorContexts.Commit)
            {
                MessageBox.Show("Commit error.");
            }
            if (anError.Context == DataGridViewDataErrorContexts.CurrentCellChange)
            {
                MessageBox.Show("Cell change.");
            }
            if (anError.Context == DataGridViewDataErrorContexts.Parsing)
            {
                MessageBox.Show("Parsing error.");
            }
            if (anError.Context == DataGridViewDataErrorContexts.LeaveControl)
            {
                MessageBox.Show("Leave control error.");
            }

            if ((anError.Exception) is ConstraintException)
            {
                DataGridView view = (DataGridView)sender;
                view.Rows[anError.RowIndex].ErrorText = "An error.";
                view.Rows[anError.RowIndex].Cells[anError.ColumnIndex].ErrorText = "An error.";

                anError.ThrowException = false;
            }
        }

    }
}
