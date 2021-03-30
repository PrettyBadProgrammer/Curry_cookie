using System;
using System.Web;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using Microsoft.VisualBasic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Text.Json;
using Newtonsoft.Json;

namespace trylogin
{
    class Program
    {
        static void Main(string[] args)
        {
            string drive_path;
            string basketid = string.Empty;
            string tokenName = string.Empty;
            string tokenValue = string.Empty;
            string myUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.150 Safari/537.36";
            string LoginUrl = "https://www.currys.co.uk/gbuk/s/authentication.html";
            string AjaxUrl = "https://www.currys.co.uk/gb/uk/groupcalls/sFuseaction/authentication/type/s/ajax.html";
            string loginEmail = "";//Email to login     
            string loginPass = "";//Password for login

            Console.WriteLine("Please enter the full drive path for the product ids, ending with .txt:");
            drive_path = Console.ReadLine();
            if (drive_path == string.Empty)
            {
                drive_path = "";//Enter default drive path
            }

            CookieContainer cookiejar = new CookieContainer();

            HttpWebRequest Ajaxreq = (HttpWebRequest)HttpWebRequest.Create(AjaxUrl);
            Ajaxreq.UserAgent = myUserAgent;
            Ajaxreq.CookieContainer = cookiejar;
            StreamReader AjaxRes = new StreamReader(Ajaxreq.GetResponse().GetResponseStream());
            string AjaxResult = AjaxRes.ReadToEnd();
            //Console.WriteLine(AjaxResult);
            AjaxRes.Close();

            HttpWebRequest wReq = (HttpWebRequest)HttpWebRequest.Create(LoginUrl);
            wReq.UserAgent = myUserAgent;
            wReq.CookieContainer = cookiejar;
            HttpWebResponse wRes = (HttpWebResponse)wReq.GetResponse();

            using (Stream LoginStream = wRes.GetResponseStream())
            {
                StreamReader reader = new StreamReader(LoginStream, Encoding.UTF8);
                string html = reader.ReadToEnd();
                // Console.WriteLine(html);
                Regex loginrx = new Regex("data-login-token-name=\"([a-zA-Z0-9]+)\"\\s+data-login-token-value=\"([a-zA-Z0-9]+)\"");
                MatchCollection loginmatch = loginrx.Matches(html);

                foreach (Match loginmatches in loginmatch)
                {
                    tokenName = loginmatches.Groups[1].ToString();
                    tokenValue = loginmatches.Groups[2].ToString();
                    //Console.WriteLine(tokenName);
                    //Console.WriteLine(tokenValue);
                }

            }

            var postData = "subaction=authentication&validate_authentication=true&sFormName=header-login&";
            postData += tokenName + "=" + tokenValue;
            postData += "&sEmail=" + HttpUtility.UrlEncode(loginEmail) + "&login=&sPassword=" + HttpUtility.UrlEncode(loginPass);
            
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
          
            var LoginReq = (HttpWebRequest)WebRequest.CreateHttp(LoginUrl);
            //var LoginReq = (HttpWebRequest)WebRequest.CreateHttp("http://www.currys.co.uk/gbuk/s/authentication.html");

            LoginReq.AllowAutoRedirect = false;
            LoginReq.Method = "POST";
            LoginReq.ContentType = "application/x-www-form-urlencoded";
            LoginReq.ContentLength = byteArray.Length;
            LoginReq.CookieContainer = cookiejar;
            LoginReq.UserAgent = myUserAgent;
            LoginReq.Headers.Add("Origin", "https://www.currys.co.uk");
            LoginReq.Headers.Add("Referer", "https://www.currys.co.uk/gbuk/s/authentication.html");
            
            Stream dataStream = LoginReq.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
         
                try
                {

                    StreamReader LoginRes = new StreamReader(LoginReq.GetResponse().GetResponseStream());
                    string LoginResult = LoginRes.ReadToEnd();
                    LoginRes.Close();

                    Console.WriteLine(LoginResult);

                }
                catch (WebException e)
                {
                    string newurl = ((HttpWebResponse)e.Response).Headers["Location"].ToString();
                    if (newurl != LoginUrl)
                    {
                        Console.WriteLine("Yay we are logged in");

                        string line;
                        List<string> productids = new List<string>();
                        try
                        {
                            StreamReader sr = new StreamReader(drive_path);

                            line = sr.ReadLine();
                            productids.Add(line);

                            while (line != null)
                            {

                                Console.WriteLine(line);

                                line = sr.ReadLine();
                                if (line != string.Empty)
                                {
                                    productids.Add(line);
                                }

                            }

                            sr.Close();
                        }
                        catch (Exception a)
                        {
                            Console.WriteLine("Exception: " + a.Message);
                        }


                        bool isthereacard = false;
                        bool CheckBasket = false;
                        string product;
                        List<string> InBasket = new List<string>();

                        while (isthereacard == false)
                        {

                            for (int x = 0; x < productids.Count - 1; x++)
                            {
                                product = productids[x];

                                if (InBasket.Contains(product) == false)
                                {

                                    try
                                    {

                                        HttpWebRequest requestLogin = (HttpWebRequest)WebRequest.Create("https://www.currys.co.uk/api/cart/addProduct");
                                        requestLogin.Method = "POST";
                                        requestLogin.CookieContainer = cookiejar;
                                        requestLogin.ContentType = "text/plain";
                                        StreamWriter stOut = new StreamWriter(requestLogin.GetRequestStream(), System.Text.Encoding.ASCII);
                                        string parameters = "{\"fupid\":\"" + product + "\",\"quantity\":1}";
                                        Console.WriteLine("Product: " + product);
                                        stOut.Write(parameters);
                                        stOut.Close();
                                        StreamReader stIn = new StreamReader(requestLogin.GetResponse().GetResponseStream());
                                        string testResults = stIn.ReadToEnd();
                                    Console.WriteLine(testResults);
                                 
                                        stIn.Close();
                                        Thread.Sleep(500);
                                        CheckBasket = true;
                                        InBasket.Add(product);

                                    }
                                    catch (Exception m)
                                    {
                                        Console.WriteLine("Is not available");
                                        isthereacard = false;
                                    }

                                }

                            }

                            if (CheckBasket)
                            {
                                var TokenRequest = (HttpWebRequest)WebRequest.CreateHttp("https://api.currys.co.uk/store/api/token");

                                TokenRequest.Method = "POST";
                                TokenRequest.CookieContainer = cookiejar;
                                TokenRequest.ContentType = "application/json; charset=utf-8";
                                TokenRequest.UserAgent = myUserAgent;
                                TokenRequest.Headers["X-Requested-With"] = "XMLHttpRequest";

                                using (var streamWriter = new StreamWriter(TokenRequest.GetRequestStream()))
                                {
                                    streamWriter.Write("{}");
                                    streamWriter.Flush();
                                }

                                HttpWebResponse response = TokenRequest.GetResponse() as HttpWebResponse;
                                using (Stream responseStream = response.GetResponseStream())
                                {
                                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                                    string json = reader.ReadToEnd();
                                    Regex rx = new Regex("\"bid\":\"([^\"]+)\"");
                                    MatchCollection match = rx.Matches(json);

                                    foreach (Match matches in match)
                                    {
                                        basketid = matches.Groups[1].ToString();
                                        //Console.WriteLine(basketid);
                                    }
                                }

                                Console.WriteLine("\nChecking basket;\n");

                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.currys.co.uk/store/api/baskets/" + basketid);
                                request.CookieContainer = cookiejar;
                                request.UserAgent = myUserAgent;
                                request.Headers.Add("Origin", "https://www.currys.co.uk");
                                request.Headers.Add("Referer", "https://www.currys.co.uk/");
                                request.Headers.Add("Sec-Fetch-Dest", "empty");
                                request.Headers.Add("Sec-Fetch-Mode", "cors");
                                request.Headers.Add("Sec-Fetch-Site", "same-site");
                                request.Headers.Add("X-Requested-With", "XMLHttpRequest");

                                HttpWebResponse BasketResponse = request.GetResponse() as HttpWebResponse;
                                string BasketData;
                                using (Stream responseStream = BasketResponse.GetResponseStream())
                                {
                                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                                    BasketData = reader.ReadToEnd();
                                    Console.WriteLine(BasketData);
                                    

                                }

                                dynamic jsondata = JsonConvert.DeserializeObject(BasketData);
                                bool available = false;
                                string productavailable = "";

                                foreach (dynamic productdata in jsondata.payload.products)
                                {
                                    foreach (dynamic status in productdata.availableFulfilmentChannels)
                                    {
                                        if (status == "home-delivery") available = true;
                                    }

                                    Console.WriteLine(productdata.id + " - " + productdata.title);

                                    if (available)
                                    {
                                        Console.WriteLine("OMG IT IS IN STOCK");
                                        productavailable = productdata.id;
                                    }
                                    else
                                    {
                                        Console.WriteLine("not in stock");
                                    }
                                }

                                if (available)
                                {
                                    foreach (dynamic productdata in jsondata.payload.products)
                                    {
                                        if (productdata.id != productavailable)
                                        {

                                            Console.WriteLine("Deleting " + productdata.id);

                                            HttpWebRequest delrequest = (HttpWebRequest)WebRequest.Create("https://api.currys.co.uk/store/api/baskets/" + basketid + "/products/" + productdata.id);
                                            delrequest.UserAgent = myUserAgent;
                                            delrequest.Headers.Add("Origin", "https://www.currys.co.uk");
                                            delrequest.Headers.Add("Referer", "https://www.currys.co.uk/");
                                            delrequest.Headers.Add("Sec-Fetch-Mode", "cors");
                                            delrequest.Headers.Add("Accept", "application/json");
                                            delrequest.Headers.Add("Content-Type", "application/json");
                                            delrequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
                                            delrequest.Method = "DELETE";
                                            delrequest.CookieContainer = cookiejar;
                                            delrequest.GetResponse();

                                            Thread.Sleep(100);

                                        }
                                    }

                                    Console.WriteLine("Ready to checkout");
                                    

                                }

                            }

                            Thread.Sleep(100);
                        }

                }
                    else
                    {
                        Console.WriteLine("Oh noooo we did not get logged in :(");
                    }

                }

        }
    }
}
