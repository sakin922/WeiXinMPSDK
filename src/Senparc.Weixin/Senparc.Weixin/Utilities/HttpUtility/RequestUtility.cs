﻿#region Apache License Version 2.0
/*----------------------------------------------------------------

Copyright 2017 Jeffrey Su & Suzhou Senparc Network Technology Co.,Ltd.

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file
except in compliance with the License. You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the
License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
either express or implied. See the License for the specific language governing permissions
and limitations under the License.

Detail: https://github.com/JeffreySu/WeiXinMPSDK/blob/master/license.md

----------------------------------------------------------------*/
#endregion Apache License Version 2.0

/*----------------------------------------------------------------
    Copyright (C) 2017 Senparc

    文件名：RequestUtility.cs
    文件功能描述：获取请求结果


    创建标识：Senparc - 20150211

    修改描述：整理接口

    修改标识：Senparc - 20150407
    修改描述：使用Post方法获取字符串结果 修改表单处理方法

    修改标识：Senparc - 20170122
    修改描述：v4.9.14 为AsUrlData方法添加null判断

    修改标识：Senparc - 20170730
    修改描述：v4.13.3 为RequestUtility.HttpGet()方法添加Accept、UserAgent、KeepAlive设置

----------------------------------------------------------------*/


    修改标识：Senparc - 20170122
    修改描述：v4.12.2 修复HttpUtility.UrlEncode方法错误
----------------------------------------------------------*/



using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Senparc.Weixin.Helpers;
#if NET45
using System.Web;
#else
using System.Net.Http;
using System.Net.Http.Headers;
#endif
#if NETSTANDARD1_6
using Microsoft.AspNetCore.Http;
#endif


namespace Senparc.Weixin.HttpUtility
{
    /// <summary>
    /// HTTP 请求工具类
    /// </summary>
    public static class RequestUtility
    {
        #region 代理

#if NET45
        private static WebProxy _webproxy = null;
        /// <summary>
        /// 设置Web代理
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public static void SetHttpProxy(string host, string port, string username, string password)
        {
            ICredentials cred;
            cred = new NetworkCredential(username, password);
            if (!string.IsNullOrEmpty(host))
            {
                _webproxy = new WebProxy(host + ":" + port ?? "80", true, null, cred);
            }
        }

        /// <summary>
        /// 清除Web代理状态
        /// </summary>
        public static void RemoveHttpProxy()
        {
            _webproxy = null;
        }
#endif

        #endregion

        #region 同步方法

        #region Get

        /// <summary>
        /// 使用Get方法获取字符串结果（没有加入Cookie）
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string HttpGet(string url, Encoding encoding = null)
        {
#if NET45
            WebClient wc = new WebClient();
            wc.Proxy = _webproxy;
            wc.Encoding = encoding ?? Encoding.UTF8;
            return wc.DownloadString(url);
#else
            HttpClient httpClient = new HttpClient();
            var t = httpClient.GetStringAsync(url);
            t.Wait();
            return t.Result;
#endif
        }

        /// <summary>
        /// 使用Get方法获取字符串结果（加入Cookie）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="encoding"></param>
        /// <param name="cer">证书，如果不需要则保留null</param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static string HttpGet(string url, CookieContainer cookieContainer = null, Encoding encoding = null, X509Certificate2 cer = null, int timeOut = Config.TIME_OUT)
        {
#if NET45
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeOut;
            request.Proxy = _webproxy;
            if (cer != null)
            {
                request.ClientCertificates.Add(cer);
            }

            if (cookieContainer != null)
            {
                request.CookieContainer = cookieContainer;
            }

            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = true;
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (cookieContainer != null)
            {
                response.Cookies = cookieContainer.GetCookies(response.ResponseUri);
            }

            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    string retString = myStreamReader.ReadToEnd();
                    return retString;
                }
            }
#else
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
            };
#if NETSTANDARD1_6
            if (cer != null)
            {
                handler.ClientCertificates.Add(cer);
            }
#endif
            HttpClient httpClient = new HttpClient(handler);
            var t = httpClient.GetStringAsync(url);
            t.Wait();
            return t.Result;
#endif
        }

        #endregion

        #region Post

        /// <summary>
        /// 使用Post方法获取字符串结果，常规提交
        /// </summary>
        /// <returns></returns>
        public static string HttpPost(string url, CookieContainer cookieContainer = null, Dictionary<string, string> formData = null, Encoding encoding = null, X509Certificate2 cer = null, int timeOut = Config.TIME_OUT)
        {
            MemoryStream ms = new MemoryStream();
            formData.FillFormDataStream(ms);//填充formData
            return HttpPost(url, cookieContainer, ms, null, null, encoding, cer, timeOut);
        }
#if NETSTANDARD1_6
        private static StreamContent CreateFileContent(Stream stream, string fileName, string contentType = "application/octet-stream")
        {
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"media\"",
                FileName = "\"" + fileName + "\""
            }; // the extra quotes are key here
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return fileContent;
        }

        private static void HttpContentHeader(HttpContent hc, int timeOut)
        {
            hc.Headers.Add("UserAgent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36");
            hc.Headers.Add("Timeout", timeOut.ToString());
            hc.Headers.Add("KeepAlive", "true");
        }
#endif
        /// <summary>
        /// 使用Post方法获取字符串结果
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="postStream"></param>
        /// <param name="fileDictionary">需要上传的文件，Key：对应要上传的Name，Value：本地文件名</param>
        /// <param name="encoding"></param>
        /// <param name="cer">证书，如果不需要则保留null</param>
        /// <param name="timeOut"></param>
        /// <param name="checkValidationResult">验证服务器证书回调自动验证</param>
        /// <param name="refererUrl"></param>
        /// <returns></returns>
        public static string HttpPost(string url, CookieContainer cookieContainer = null, Stream postStream = null, Dictionary<string, string> fileDictionary = null, string refererUrl = null, Encoding encoding = null, X509Certificate2 cer = null, int timeOut = Config.TIME_OUT, bool checkValidationResult = false)
        {

#if NET45
            if (checkValidationResult)
            {
                ServicePointManager.ServerCertificateValidationCallback =
                  new RemoteCertificateValidationCallback(CheckValidationResult);
            }
#endif

            if (cookieContainer == null)
                cookieContainer = new CookieContainer();
#if NET45

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Timeout = timeOut;
            request.Proxy = _webproxy;
            if (cer != null)
            {
                request.ClientCertificates.Add(cer);
            }
#else
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookieContainer;

            if (checkValidationResult)
                handler.ServerCertificateCustomValidationCallback = new Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>(CheckValidationResult);

            if (cer != null)
            {
                handler.ClientCertificates.Add(cer);
            }

            HttpClient client = new HttpClient(handler);

            HttpContent hc;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
#endif
            #region 处理Form表单文件上传
            var formUploadFile = fileDictionary != null && fileDictionary.Count > 0;//是否用Form上传文件
            if (formUploadFile)
            {

                //通过表单上传文件
                string boundary = "----" + DateTime.Now.Ticks.ToString("x");
#if NET45
                postStream = postStream ?? new MemoryStream();
                //byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                string fileFormdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
                string dataFormdataTemplate = "\r\n--" + boundary +
                                                "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
#else
                hc = new MultipartFormDataContent(boundary);
                HttpContentHeader(hc, timeOut);
#endif

                foreach (var file in fileDictionary)
                {
                    try
                    {
                        var fileName = file.Value;
                        //准备文件流
                        using (var fileStream = FileHelper.GetFileStream(fileName))
                        {
#if NET45
                            string formdata = null;
                            if (fileStream != null)
                            {
                                //存在文件
                                formdata = string.Format(fileFormdataTemplate, file.Key, /*fileName*/ Path.GetFileName(fileName));
                            }
                            else
                            {
                                //不存在文件或只是注释
                                formdata = string.Format(dataFormdataTemplate, file.Key, file.Value);
                            }

                            //统一处理
                            var formdataBytes = Encoding.UTF8.GetBytes(postStream.Length == 0 ? formdata.Substring(2, formdata.Length - 2) : formdata);//第一行不需要换行
                            postStream.Write(formdataBytes, 0, formdataBytes.Length);

                            //写入文件
                            if (fileStream != null)
                            {
                                byte[] buffer = new byte[1024];
                                int bytesRead = 0;
                                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                                {
                                    postStream.Write(buffer, 0, bytesRead);
                                }
                            }
#else
                            if (fileStream != null)
                            {
                                //存在文件
                                //hc.Add(new StreamContent(fileStream), file.Key, Path.GetFileName(fileName)); //报流已关闭的异常
                                fileStream.Dispose();
                                (hc as MultipartFormDataContent).Add(CreateFileContent(File.Open(fileName, FileMode.Open), Path.GetFileName(fileName)), file.Key, Path.GetFileName(fileName));
                            }
                            else
                            {
                                //不存在文件或只是注释
                                (hc as MultipartFormDataContent).Add(new StringContent(string.Empty), file.Key, file.Value);
                            }
#endif
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
#if NET45
                //结尾
                var footer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
                postStream.Write(footer, 0, footer.Length);

                request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);
#else
                hc.Headers.ContentType = MediaTypeHeaderValue.Parse(string.Format("multipart/form-data; boundary={0}", boundary));
#endif
            }
            else
            {
#if NET45
                request.ContentType = "application/x-www-form-urlencoded";
#else
                hc = new StreamContent(postStream);
                HttpContentHeader(hc, timeOut);
                hc.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
#endif
            }
            #endregion

#if NET45

            request.ContentLength = postStream != null ? postStream.Length : 0;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = true;

            if (!string.IsNullOrEmpty(refererUrl))
            {
                request.Referer = refererUrl;
            }
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";

            if (cookieContainer != null)
            {
                request.CookieContainer = cookieContainer;
            }

            #region 输入二进制流
            if (postStream != null)
            {
                postStream.Position = 0;

                //直接写入流
                Stream requestStream = request.GetRequestStream();

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = postStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                }

                //debug
                //postStream.Seek(0, SeekOrigin.Begin);
                //StreamReader sr = new StreamReader(postStream);
                //var postStr = sr.ReadToEnd();

                postStream.Close();//关闭文件访问
            }
            #endregion

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (cookieContainer != null)
            {
                response.Cookies = cookieContainer.GetCookies(response.ResponseUri);
            }

            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    string retString = myStreamReader.ReadToEnd();
                    return retString;
                }
            }
#else
            //TODO:Cookie
            var t = client.PostAsync(url, hc);
            t.Wait();
            var t1 = t.Result.Content.ReadAsStringAsync();
            t1.Wait();
            return t1.Result;
#endif


        }

        #endregion

        /// <summary>
        /// 验证服务器证书
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
#if NETSTANDARD1_6
        /// <summary>
        /// 验证服务器证书
        /// </summary>
        /// <param name="request"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        private static bool CheckValidationResult(HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
#endif
        #endregion

        #region 异步方法

        /// <summary>
        /// 使用Get方法获取字符串结果（没有加入Cookie）
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> HttpGetAsync(string url, Encoding encoding = null)
        {


#if NET45
            WebClient wc = new WebClient();
            wc.Proxy = _webproxy;
            wc.Encoding = encoding ?? Encoding.UTF8;
            return await wc.DownloadStringTaskAsync(url);
#else
            HttpClient httpClient = new HttpClient();
            return await httpClient.GetStringAsync(url);
#endif

        }

        /// <summary>
        /// 使用Get方法获取字符串结果（加入Cookie）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="encoding"></param>
        /// <param name="cer">证书，如果不需要则保留null</param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static async Task<string> HttpGetAsync(string url, CookieContainer cookieContainer = null, Encoding encoding = null, X509Certificate2 cer = null, int timeOut = Config.TIME_OUT)
        {
#if NET45
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeOut;
            request.Proxy = _webproxy;
            if (cer != null)
            {
                request.ClientCertificates.Add(cer);
            }

            if (cookieContainer != null)
            {
                request.CookieContainer = cookieContainer;
            }

            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());

            if (cookieContainer != null)
            {
                response.Cookies = cookieContainer.GetCookies(response.ResponseUri);
            }

            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    string retString = await myStreamReader.ReadToEndAsync();
                    return retString;
                }
            }

#else
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
            };
#if NETSTANDARD1_6
            if (cer != null)
            {
                handler.ClientCertificates.Add(cer);
            }
#endif

            HttpClient httpClient = new HttpClient(handler);
            return await httpClient.GetStringAsync(url);
#endif
        }

        /// <summary>
        /// 使用Post方法获取字符串结果，常规提交
        /// </summary>
        /// <returns></returns>
        public static async Task<string> HttpPostAsync(string url, CookieContainer cookieContainer = null, Dictionary<string, string> formData = null, Encoding encoding = null, X509Certificate2 cer = null, int timeOut = Config.TIME_OUT)
        {
#if NET45

            MemoryStream ms = new MemoryStream();
            await formData.FillFormDataStreamAsync(ms);//填充formData
            return await HttpPostAsync(url, cookieContainer, ms, null, null, encoding, cer, timeOut);
#else
            MemoryStream ms = new MemoryStream();
            await formData.FillFormDataStreamAsync(ms);//填充formData
            return await HttpPostAsync(url, cookieContainer, ms, null, null, encoding, cer, timeOut);

#endif

        }


        /// <summary>
        /// 使用Post方法获取字符串结果
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="postStream"></param>
        /// <param name="fileDictionary">需要上传的文件，Key：对应要上传的Name，Value：本地文件名</param>
        /// <param name="timeOut"></param>
        /// <param name="checkValidationResult">验证服务器证书回调自动验证</param>
        /// <returns></returns>
        public static async Task<string> HttpPostAsync(string url, CookieContainer cookieContainer = null, Stream postStream = null, Dictionary<string, string> fileDictionary = null, string refererUrl = null, Encoding encoding = null, X509Certificate2 cer = null, int timeOut = Config.TIME_OUT, bool checkValidationResult = false)
        {

#if NET45
            if (checkValidationResult)
            {
                ServicePointManager.ServerCertificateValidationCallback =
                  new RemoteCertificateValidationCallback(CheckValidationResult);
            }
#endif
            if (cookieContainer == null)
                cookieContainer = new CookieContainer();

#if NET45

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Timeout = timeOut;
            request.Proxy = _webproxy;
            if (cer != null)
            {
                request.ClientCertificates.Add(cer);
            }
#else
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookieContainer;

            if (checkValidationResult)
                handler.ServerCertificateCustomValidationCallback = new Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>(CheckValidationResult);

            if (cer != null)
            {
                handler.ClientCertificates.Add(cer);
            }

            HttpClient client = new HttpClient(handler);
            HttpContent hc;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));

#endif

            #region 处理Form表单文件上传
            var formUploadFile = fileDictionary != null && fileDictionary.Count > 0;//是否用Form上传文件
            if (formUploadFile)
            {
                //通过表单上传文件
                string boundary = "----" + DateTime.Now.Ticks.ToString("x");
#if NET45
                postStream = postStream ?? new MemoryStream();
                
                //byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                string fileFormdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
                string dataFormdataTemplate = "\r\n--" + boundary +
                                              "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
#else
                hc = new MultipartFormDataContent(boundary);
                HttpContentHeader(hc, timeOut);
#endif
                foreach (var file in fileDictionary)
                {
                    try
                    {
                        var fileName = file.Value;
                        //准备文件流
                        using (var fileStream = FileHelper.GetFileStream(fileName))
                        {
#if NET45
                            string formdata = null;
                            if (fileStream != null)
                            {
                                //存在文件
                                formdata = string.Format(fileFormdataTemplate, file.Key, /*fileName*/ Path.GetFileName(fileName));
                            }
                            else
                            {
                                //不存在文件或只是注释
                                formdata = string.Format(dataFormdataTemplate, file.Key, file.Value);
                            }

                            //统一处理
                            var formdataBytes = Encoding.UTF8.GetBytes(postStream.Length == 0 ? formdata.Substring(2, formdata.Length - 2) : formdata);//第一行不需要换行
                            await postStream.WriteAsync(formdataBytes, 0, formdataBytes.Length);

                            //写入文件
                            if (fileStream != null)
                            {
                                byte[] buffer = new byte[1024];
                                int bytesRead = 0;
                                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                                {
                                    await postStream.WriteAsync(buffer, 0, bytesRead);
                                }
                            }
#else
                            if (fileStream != null)
                            {
                                //存在文件
                                //hc.Add(new StreamContent(fileStream), file.Key, Path.GetFileName(fileName)); //报流已关闭的异常
                                fileStream.Dispose();
                                (hc as MultipartFormDataContent).Add(CreateFileContent(File.Open(fileName, FileMode.Open), Path.GetFileName(fileName)), file.Key, Path.GetFileName(fileName));
                            }
                            else
                            {
                                //不存在文件或只是注释
                                (hc as MultipartFormDataContent).Add(new StringContent(string.Empty), file.Key, file.Value);
                            }
#endif
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
#if NET45
                //结尾
                var footer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
                await postStream.WriteAsync(footer, 0, footer.Length);

                request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);
#else
                hc.Headers.ContentType = MediaTypeHeaderValue.Parse(string.Format("multipart/form-data; boundary={0}", boundary));
#endif
            }
            else
            {
#if NET45
                request.ContentType = "application/x-www-form-urlencoded";
#else
                hc = new StreamContent(postStream);
                HttpContentHeader(hc, timeOut);
                hc.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
#endif

            }
            #endregion

#if NET45
            request.ContentLength = postStream != null ? postStream.Length : 0;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = true;

            if (!string.IsNullOrEmpty(refererUrl))
            {
                request.Referer = refererUrl;
            }
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";

            if (cookieContainer != null)
            {
                request.CookieContainer = cookieContainer;
            }

            #region 输入二进制流
            if (postStream != null)
            {
                postStream.Position = 0;

                //直接写入流
                Stream requestStream = await request.GetRequestStreamAsync();

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = await postStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    await requestStream.WriteAsync(buffer, 0, bytesRead);
                }


                //debug
                //postStream.Seek(0, SeekOrigin.Begin);
                //StreamReader sr = new StreamReader(postStream);
                //var postStr = await sr.ReadToEndAsync();

                postStream.Close();//关闭文件访问
            }
            #endregion

            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());

            if (cookieContainer != null)
            {
                response.Cookies = cookieContainer.GetCookies(response.ResponseUri);
            }

            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    string retString = await myStreamReader.ReadToEndAsync();
                    return retString;
                }
            }
#else
            //TODO:Cookie
            var r = await client.PostAsync(url, hc);
            return await r.Content.ReadAsStringAsync();

#endif

        }


        /// <summary>
        /// 填充表单信息的Stream
        /// </summary>
        /// <param name="formData"></param>
        /// <param name="stream"></param>
        public static async Task FillFormDataStreamAsync(this Dictionary<string, string> formData, Stream stream)
        {
            string dataString = GetQueryString(formData);
            var formDataBytes = formData == null ? new byte[0] : Encoding.UTF8.GetBytes(dataString);
            await stream.WriteAsync(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);//设置指针读取位置
        }

        #endregion

        ///// <summary>
        ///// 请求是否发起自微信客户端的浏览器
        ///// </summary>
        ///// <param name="httpContext"></param>
        ///// <returns></returns>
        //[Obsolete("请使用Senparc.Weixin.BrowserUtility.BrowserUtility.SideInWeixinBrowser()方法")]
        //public static bool IsWeixinClientRequest(this HttpContext httpContext)
        //{
        //    return !string.IsNullOrEmpty(httpContext.Request.UserAgent) &&
        //           httpContext.Request.UserAgent.Contains("MicroMessenger");
        //}

        /// <summary>
        /// 组装QueryString的方法
        /// 参数之间用&amp;连接，首位没有符号，如：a=1&amp;b=2&amp;c=3
        /// </summary>
        /// <param name="formData"></param>
        /// <returns></returns>
        public static string GetQueryString(this Dictionary<string, string> formData)
        {
            if (formData == null || formData.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();

            var i = 0;
            foreach (var kv in formData)
            {
                i++;
                sb.AppendFormat("{0}={1}", kv.Key, kv.Value);
                if (i < formData.Count)
                {
                    sb.Append("&");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 填充表单信息的Stream
        /// </summary>
        /// <param name="formData"></param>
        /// <param name="stream"></param>
        public static void FillFormDataStream(this Dictionary<string, string> formData, Stream stream)
        {
            string dataString = GetQueryString(formData);
            var formDataBytes = formData == null ? new byte[0] : Encoding.UTF8.GetBytes(dataString);
            stream.Write(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);//设置指针读取位置
        }

        /// <summary>
        /// 封装System.Web.HttpUtility.HtmlEncode
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string HtmlEncode(this string html)
        {
#if NET45
            return System.Web.HttpUtility.HtmlEncode(html);
#else
            return WebUtility.HtmlEncode(html);
#endif
        }
        /// <summary>
        /// 封装System.Web.HttpUtility.HtmlDecode
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string HtmlDecode(this string html)
        {
#if NET45
            return System.Web.HttpUtility.HtmlDecode(html);
#else
            return WebUtility.HtmlDecode(html);
#endif
        }
        /// <summary>
        /// 封装System.Web.HttpUtility.UrlEncode
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string UrlEncode(this string url)
        {
#if NET45
            return System.Web.HttpUtility.UrlEncode(url);
#else
            return WebUtility.UrlEncode(url);
#endif
        }
        /// <summary>
        /// 封装System.Web.HttpUtility.UrlDecode
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string UrlDecode(this string url)
        {
#if NET45
            return System.Web.HttpUtility.UrlDecode(url);
#else
            return WebUtility.UrlDecode(url);
#endif

        }

        /// <summary>
        /// <para>将 URL 中的参数名称/值编码为合法的格式。</para>
        /// <para>可以解决类似这样的问题：假设参数名为 tvshow, 参数值为 Tom&Jerry，如果不编码，可能得到的网址： http://a.com/?tvshow=Tom&Jerry&year=1965 编码后则为：http://a.com/?tvshow=Tom%26Jerry&year=1965 </para>
        /// <para>实践中经常导致问题的字符有：'&', '?', '=' 等</para>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string AsUrlData(this string data)
        {
            if (data == null)
            {
                return null;
            }
            return Uri.EscapeDataString(data);
        }
    }
}
