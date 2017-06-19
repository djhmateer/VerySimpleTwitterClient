using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VerySimpleTwitterClient
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Starting TwitterStreamLoader");
            new TwitterStreamClient().CallTwitterStreamingAPI();
        }
    }

    public class TwitterStreamClient : OAuthBase
    {
        string filteredUrl = @"https://stream.twitter.com/1.1/statuses/filter.json";

        // *** PUT IN YOUR KEYS HERE - get from http://apps.twitter.com ****
        string consumerKey = "DdIGMxxxxxxxxxx";
        string consumerSecret = "ELzyK85xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
        string accessToken = "11309782-s2LPhVyHxxxxxxxxxxxxxxxxxxxxxxxxx";
        string accessSecret = "sEAGOB7mWVTo6lxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

        public void CallTwitterStreamingAPI()
        {
            while (true)
            {
                // docs: https://dev.twitter.com/streaming/overview/request-parameters
                var keywords = new List<string>();
                //keywords.AddRange(new List<string> { "Entomology", "Bumble Bee", "bumblebees" });
                keywords.AddRange(new List<string> { "Scotland" });
                var keywordsEncoded = UrlEncode(string.Join(",", keywords.ToArray()));

                // http://gettwitterid.com
                //var follows = new List<string>();
                //follows.Add("833003030089957380"); //pjhemingway
                //var followEncoded = UrlEncode(string.Join(",", follows.ToArray()));

                var postParameters =
                                //"&locations=" + UrlEncode("-7,50,3,60") // UK 
                                //"&locations=" + UrlEncode("-180,-90,180,90") // World
                                ("&track=" + keywordsEncoded) // Keywords
                                //+ ("&follow=" + followEncoded) // People
                                ;

                if (string.IsNullOrEmpty(postParameters)) { }
                else if (postParameters.IndexOf('&') == 0)
                    postParameters = postParameters.Remove(0, 1).Replace("#", "%23");

                try
                {
                    var webRequest = (HttpWebRequest)WebRequest.Create(filteredUrl);
                    webRequest.Timeout = -1;
                    webRequest.Headers.Add("Authorization", GetAuthHeader(filteredUrl + "?" + postParameters));

                    var encode = Encoding.GetEncoding("utf-8");
                    webRequest.Method = "POST";
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    var twitterTrack = encode.GetBytes(postParameters);
                    webRequest.ContentLength = twitterTrack.Length;
                    var twitterPost = webRequest.GetRequestStream();
                    twitterPost.Write(twitterTrack, 0, twitterTrack.Length);
                    twitterPost.Close();

                    webRequest.BeginGetResponse(ar =>
                    {
                        var req = (WebRequest)ar.AsyncState;
                        using (var response = req.EndGetResponse(ar))
                        {
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                while (!reader.EndOfStream)
                                {
                                    var json = reader.ReadLine();

                                    if (!string.IsNullOrEmpty(json))
                                        Console.WriteLine(json.Substring(109, Math.Min(json.Length, 80)));

                                    Console.WriteLine();
                                    //DoSomething(json);
                                }
                            }
                        }

                    }, webRequest);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Thread.Sleep(250);
                }
            }
        }

        public void DoSomething(string json)
        {
            Tweet tweet;
            try
            {
                tweet = DeserialiseJsonToTweet(json);
            }
            catch (Exception ex)
            {
                // Deserialisation failed
                return;
            }

            // Save to DB
        }

        public static Tweet DeserialiseJsonToTweet(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("Cannot deserialise a null or empty string");

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IsoDateTimeConverter
            {
                DateTimeFormat = "ddd MMM dd HH:mm:ss +ffff yyyy",
                DateTimeStyles = DateTimeStyles.AdjustToUniversal
            });
            var tweet = JsonConvert.DeserializeObject<Tweet>(json, settings);

            if (tweet.id_str == null)
                throw new ArgumentException("Valid json but not a valid tweet, as no ID found");

            return tweet;
        }


        // Uses OAuthBase
        private string GetAuthHeader(string url)
        {
            var timeStamp = GenerateTimeStamp();
            var nonce = GenerateNonce();

            string normalizeUrl;
            string normalizedString;
            string oauthSignature = GenerateSignature(new Uri(url), consumerKey, consumerSecret, accessToken, accessSecret,
                "POST", timeStamp, nonce, out normalizeUrl, out normalizedString);

            const string headerFormat = "OAuth oauth_nonce=\"{0}\", oauth_signature_method=\"{1}\", " +
                                        "oauth_timestamp=\"{2}\", oauth_consumer_key=\"{3}\", " +
                                        "oauth_token=\"{4}\", oauth_signature=\"{5}\", " +
                                        "oauth_version=\"{6}\"";

            return string.Format(headerFormat,
                Uri.EscapeDataString(nonce),
                Uri.EscapeDataString(Hmacsha1SignatureType),
                Uri.EscapeDataString(timeStamp),
                Uri.EscapeDataString(consumerKey),
                Uri.EscapeDataString(accessToken),
                Uri.EscapeDataString(oauthSignature),
                Uri.EscapeDataString(OAuthVersion));
        }
    }

    // This code is based on
    // http://www.adamjamesbull.co.uk/words/rolling-your-own-connecting-to-the-twitter-streaming-api-using-c/
}
