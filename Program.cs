using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using System.Diagnostics;
using System.Linq;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using RestSharp;

namespace _3080Finder
{
    class Site
    {
        public Enum id;
        public string url;
        public string searchString;

        public Site(Enum id, string url, string searchString)
        {
            this.id = id;
            this.url = url;
            this.searchString = searchString;
        }

    }

    class Program
    {
        enum SiteId
        {
            Nvidia,
            Evga,
            Newegg,
            BH,
            Amazon, 
            Asus,
            Zotac,
            BestBuy, 
            MicroCenter
        };
        
        static void Main(string[] args)
        {
            while (true)
            {
                Program locator = new Program();
                locator.Search();

                Thread.Sleep(5000);
            }

        }

        public void Search()
        {
            List<Site> sitesToCheck = GatherSites();
            bool inStock;

            Console.WriteLine("\nChecking Sites " + DateTime.Now);

            foreach (Site currentSite in sitesToCheck)
            {
                Console.WriteLine("Checking " + Enum.GetName(typeof(SiteId), currentSite.id) + "...");
                if (currentSite.id.Equals(SiteId.Nvidia))
                {
                    inStock = NvidiaAPIGet(currentSite);
                }
                else
                {
                    inStock = ScrapeHTML(currentSite);
                }

                if (inStock)
                {
                    Console.WriteLine("------------In Stock------------");
                    OpenSite(currentSite);
                    NotifyViaToast(currentSite);
                    NotifyViaTwilio(currentSite);
                    Thread.Sleep(300000);
                }
            }
        }

        public bool ScrapeHTML(Site currentSite)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    string htmlCode = client.DownloadString(currentSite.url);
                    bool inStock = htmlCode.Contains(currentSite.searchString);

                    return inStock;
                }

                catch (Exception ex)
                {
                    Console.WriteLine("     Scrape Error");
                    Console.WriteLine("           " + ex.Message);
                    return false;
                }

            }

        }
        public List<Site> GatherSites()
        {
            List<Site> sitesToCheck = new List<Site>();

            //EVGA  No longer selling due to queue system
            //sitesToCheck.Add(new Site(SiteId.Evga, "https://www.evga.com/products/ProductList.aspx?type=0&family=GeForce+30+Series+Family&chipset=RTX+3080", "AddCart"));
            //NewEgg
            sitesToCheck.Add(new Site(SiteId.Newegg, "https://www.newegg.com/p/pl?N=100007709%20601357247%2050001315%2050001402&d=rtx+3080", "Add to cart"));
            //B&H No longer selling 3080
            //sitesToCheck.Add(new Site(SiteId.BH, "https://www.bhphotovideo.com/c/search?q=3080&filters=fct_category%3Agraphic_cards_6567", "Add to Cart"));
            //Nividia No longer selling directly
            //sitesToCheck.Add(new Site(SiteId.Nvidia, "https://www.nvidia.com/en-us/geforce/graphics-cards/30-series/rtx-3080/"));
            //Amazon
            sitesToCheck.Add(new Site(SiteId.Amazon, "https://www.amazon.com/stores/page/6B204EA4-AAAC-4776-82B1-D7C3BD9DDC82?ingress=0", ">Add to Cart<"));
            //Asus
            sitesToCheck.Add(new Site(SiteId.Asus, "https://store.asus.com/us/search?q=3080&s_c=1", ">Buy Now<"));
            //MicroCenter
            sitesToCheck.Add(new Site(SiteId.MicroCenter, "https://www.microcenter.com/search/search_results.aspx?N=&cat=&Ntt=3080&searchButton=search", "IN STOCK"));
            //Zotac
            //sitesToCheck.Add(new Site(SiteId.Zotac, "https://store.zotac.com/graphics-cards/geforce-rtx-30-series/geforce-rtxtm-3080-1", "ADD TO CART"));
            //BestBuy
            //sitesToCheck.Add(new Site(SiteId.BestBuy, "https://www.bestbuy.com/site/searchpage.jsp?st=3080", "btn btn-primary btn-sm btn-block btn-leading-ficon add-to-cart-button"));


            return sitesToCheck;

        }

        public void OpenSite(Site currentSite)
        {
            var ps = new ProcessStartInfo(currentSite.url)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }

        public void NotifyViaToast(Site currentSite)
        {
            string notificationTitle = "3080 in Stock at " + Enum.GetName(typeof(SiteId), currentSite.id);

            XmlDocument template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);

            var textNodes = template.GetElementsByTagName("text").ToList();
            foreach (var textNode in textNodes)
            {
                textNode.AppendChild(template.CreateTextNode(notificationTitle));
            }

            var toast = new ToastNotification(template);
            toast.Tag = "Console App";
            toast.Group = "C#";
            toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);

            var notifier = ToastNotificationManager.CreateToastNotifier("ConsoleToast");
            notifier.Show(toast);
        }

        public void NotifyViaTwilio(Site currentSite)
        {
            string messageBody = "3080 In Stock at " + Enum.GetName(typeof(SiteId), currentSite.id) + "\n " + currentSite.url;
            Security security = new Security();

            TwilioClient.Init(security.AccountSid, security.AuthToken);

            var message = MessageResource.Create(
                body: messageBody,
                from: new Twilio.Types.PhoneNumber(security.FromPhoneNumber),
                to: new Twilio.Types.PhoneNumber(security.ToPhoneNumber)
            );
        }

        public bool NvidiaAPIGet(Site currentSite)
        {
            string NvidiaEndPoint = "https://api-prod.nvidia.com/direct-sales-shop/DR/products/en_us/USD/5438481700";
            var client = new RestClient(NvidiaEndPoint);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("        Error with Nvidia API Call ***********");
                Console.WriteLine("            Status: " + response.StatusCode);
                return false;

            }
            return !response.Content.Contains("PRODUCT_INVENTORY_OUT_OF_STOCK");
            
        }
    }    
}
