﻿using System;
using System.Reflection;
using System.Resources;
using System.ServiceModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business.SpiraTeam_Client;
using Inflectra.Global; 
using Microsoft.VisualStudio.Shell.Interop;
using System.Linq;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business.Properties;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business
{
    public static partial  class StaticFuncs
	{
		private static ResourceManager _internalManager;
		private static Dictionary<string, BitmapSource> storedImgs = new Dictionary<string, BitmapSource>();

		/// <summary>Readonly. Returns the resource manager for the loaded library.</summary>
		public static ResourceManager getCultureResource
		{
			get
			{
				if (StaticFuncs._internalManager == null)
				{
					Assembly addinAssembly = Assembly.GetExecutingAssembly();
					Assembly satelliteAssembly = addinAssembly.GetSatelliteAssembly(System.Globalization.CultureInfo.GetCultureInfo("en-US"));
					string resAssName = "";
					foreach (string resName in satelliteAssembly.GetManifestResourceNames())
					{
						if (resName.ToLowerInvariant().Trim() == "Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Resources.Properties.Resources.resources".ToLowerInvariant())
							resAssName = resName.Substring(0, resName.LastIndexOf("."));
					}

					try
					{
						StaticFuncs._internalManager = new ResourceManager(resAssName, satelliteAssembly);
					}
					catch
					{
						StaticFuncs._internalManager = null;
					}
				}

				return StaticFuncs._internalManager;
			}
		}

		/// <summary>Returns the environment window.</summary>
		/// <returns>DTE2 object</returns>
		public static EnvDTE80.DTE2 GetEnvironment
		{
			get
			{
				return (EnvDTE80.DTE2)Package.GetGlobalService(typeof(SDTE));
			}
		}

		/// <summary>Creates an Image control for a specified resource.</summary>
		/// <param name="Key">The key name of the resource to use. Will search and use Product-dependent resources first.</param>
		/// <param name="Size">Size of the desired image, or null.</param>
		/// <param name="Stretch">Desired stretch setting of image, or null.</param>
		/// <returns>Resulting image, or null if key is not found.</returns>
		public static System.Windows.Controls.Image getImage(string key, System.Windows.Size size)
		{
			Image retImage = new System.Windows.Controls.Image();

			try
			{
				if (StaticFuncs.storedImgs.ContainsKey(key))
				{
					retImage.Source = StaticFuncs.storedImgs[key];
				}
				else
				{
					if (!size.IsEmpty && (size.Height != 0 && size.Width != 0))
					{
						retImage.Height = size.Height;
						retImage.Width = size.Width;
					}

					BitmapSource image = null;

					System.Drawing.Bitmap imgBmp = (System.Drawing.Bitmap)StaticFuncs.getCultureResource.GetObject(key);

					if (imgBmp != null)
					{
						IntPtr bmStream = imgBmp.GetHbitmap();
						System.Windows.Int32Rect rect = new Int32Rect(0, 0, imgBmp.Width, imgBmp.Height);

						image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmStream, IntPtr.Zero, rect, BitmapSizeOptions.FromWidthAndHeight(imgBmp.Width, imgBmp.Height));
					}

					retImage.Source = image;

					//Add it to our library for use later..
					storedImgs.Add(key, image);
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex);
			}

			return retImage;
		}

		/// <summary>Creates a web client for use.</summary>
		/// <param name="serverAddress">The base address of the server.</param>
		/// <returns>ImportExportClient</returns>
		public static SoapServiceClient CreateClient(string serverAddress)
		{
            SoapServiceClient retClient = null;

			try
			{
				//The endpoint address.
				EndpointAddress EndPtAddr = new EndpointAddress(new Uri(serverAddress + Settings.Default.app_ServiceURI));
				//Create the soap client.
				BasicHttpBinding wsDualHttp = new BasicHttpBinding();
				wsDualHttp.CloseTimeout = TimeSpan.FromMinutes(1);
				wsDualHttp.OpenTimeout = TimeSpan.FromMinutes(1);
				wsDualHttp.ReceiveTimeout = TimeSpan.FromMinutes(1);
				wsDualHttp.SendTimeout = TimeSpan.FromMinutes(1);
				wsDualHttp.BypassProxyOnLocal = false;
				wsDualHttp.HostNameComparisonMode = HostNameComparisonMode.StrongWildcard;
				wsDualHttp.MaxBufferPoolSize = Int32.MaxValue;
				wsDualHttp.MaxReceivedMessageSize = Int32.MaxValue;
				wsDualHttp.MessageEncoding = WSMessageEncoding.Text;
				wsDualHttp.TextEncoding = Encoding.UTF8;
				wsDualHttp.UseDefaultWebProxy = true;
				wsDualHttp.ReaderQuotas.MaxDepth = Int32.MaxValue;
				wsDualHttp.ReaderQuotas.MaxStringContentLength = Int32.MaxValue;
				wsDualHttp.ReaderQuotas.MaxArrayLength = Int32.MaxValue;
				wsDualHttp.ReaderQuotas.MaxBytesPerRead = Int32.MaxValue;
				wsDualHttp.ReaderQuotas.MaxNameTableCharCount = Int32.MaxValue;
				wsDualHttp.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.Certificate;
				wsDualHttp.Security.Message.AlgorithmSuite = System.ServiceModel.Security.SecurityAlgorithmSuite.Default;
				wsDualHttp.Security.Mode = BasicHttpSecurityMode.None;
				wsDualHttp.AllowCookies = true;
				wsDualHttp.TransferMode = TransferMode.Streamed;
				//Configure for alternative connection types.
				if (EndPtAddr.Uri.Scheme == "https")
				{
					wsDualHttp.Security.Mode = BasicHttpSecurityMode.Transport;
					wsDualHttp.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;

					//Allow self-signed certificates
					Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business.Spira_ImportExport.PermissiveCertificatePolicy.Enact("");
				}

				retClient = new SpiraTeam_Client.SoapServiceClient(wsDualHttp, EndPtAddr);
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex);
				retClient = null;
			}

			return retClient;

		}

		/// <summary>Generates a pretty Error message string.</summary>
		/// <param name="e">Exception.</param>
		/// <returns>String of the error messages.</returns>
		public static string getErrorMessage(Exception e)
		{
			string errMsg = "» " + e.Message;
			while (e.InnerException != null)
			{
				errMsg += Environment.NewLine + "» " + e.InnerException.Message;
				e = e.InnerException;
			}

			return errMsg;
		}

        /// <summary>
        /// Renders HTML content as plain text, since JIRA cannot handle tags
        /// </summary>
        /// <param name="source">The HTML markup</param>
        /// <returns>Plain text representation</returns>
        /// <remarks>Handles line-breaks, etc.</remarks>
        public static string HtmlRenderAsPlainText(this string source)
        {
            try
            {
                string result;

                // Remove HTML Development formatting
                // Replace line breaks with space
                // because browsers inserts space
                result = source.Replace("\r", " ");
                // Replace line breaks with space
                // because browsers inserts space
                result = result.Replace("\n", " ");
                // Remove step-formatting
                result = result.Replace("\t", string.Empty);
                // Remove repeating speces becuase browsers ignore them
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"( )+", " ");

                // Remove the header (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<( )*head([^>])*>", "<head>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"(<( )*(/)( )*head( )*>)", "</head>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    "(<head>).*(</head>)", string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // remove all scripts (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<( )*script([^>])*>", "<script>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"(<( )*(/)( )*script( )*>)", "</script>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                //result = System.Text.RegularExpressions.Regex.Replace(result, 
                //         @"(<script>)([^(<script>\.</script>)])*(</script>)",
                //         string.Empty, 
                //         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"(<script>).*(</script>)", string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // remove all styles (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<( )*style([^>])*>", "<style>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"(<( )*(/)( )*style( )*>)", "</style>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    "(<style>).*(</style>)", string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert tabs in spaces of <td> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<( )*td([^>])*>", "\t",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert line breaks in places of <BR> and <LI> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<( )*br( )*>", "\r",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<( )*br( )*/>", "\r",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<( )*li( )*>", "\r",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert line paragraphs (double line breaks) in place
                // if <P>, <DIV> and <TR> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<( )*div([^>])*>", "\r\r",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<( )*tr([^>])*>", "\r\r",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<( )*p([^>])*>", "\r\r",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Remove remaining tags like <a>, links, images,
                // comments etc - anything thats enclosed inside < >
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"<[^>]*>", string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                //HTML Decode to convert &xxxx; entities back to text
                result = System.Web.HttpUtility.HtmlDecode(result);

                // make line breaking consistent
                result = result.Replace("\n", "\r");

                // Remove extra line breaks and tabs:
                // replace over 2 breaks with 2 and over 4 tabs with 4. 
                // Prepare first to remove any whitespaces inbetween
                // the escaped characters and remove redundant tabs inbetween linebreaks
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    "(\r)( )+(\r)", "\r\r",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    "(\t)( )+(\t)", "\t\t",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    "(\t)( )+(\r)", "\t\r",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    "(\r)( )+(\t)", "\r\t",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove redundant tabs
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    "(\r)(\t)+(\r)", "\r\r",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove multible tabs followind a linebreak with just one tab
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    "(\r)(\t)+", "\r\t",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Initial replacement target string for linebreaks
                string breaks = "\r\r\r";
                // Initial replacement target string for tabs
                string tabs = "\t\t\t\t\t";
                for (int index = 0; index < result.Length; index++)
                {
                    result = result.Replace(breaks, "\r\r");
                    result = result.Replace(tabs, "\t\t\t\t");
                    breaks = breaks + "\r";
                    tabs = tabs + "\t";
                }

                //Convert newlines into a form that JIRA likes
                return result.Replace("\r", "\r\n");
            }
            catch
            {
                return source;
            }
        }


        /// <summary>Remove HTML tags from a string.</summary>
        public static string StripTagsCharArray(string source)
		{
			if (source == null) source = "";
			char[] array = new char[source.Length];
			int arrayIndex = 0;
			bool inside = false;

			for (int i = 0; i < source.Length; i++)
			{
				char let = source[i];
				if (let == '<')
				{
					inside = true;
					continue;
				}
				if (let == '>')
				{
					inside = false;
					continue;
				}
				if (!inside)
				{
					array[arrayIndex] = let;
					arrayIndex++;
				}
			}
			return new string(array, 0, arrayIndex);
		}

		/// <summary>Returns the value in minutes from the given string values.</summary>
		/// <param name="Hours">String from the Hours textbox.</param>
		/// <param name="Minutes">String from the Minutes textbox.</param>
		/// <param name="AllowNull">Whether or not to allow returning of a null value. Defaukt - Yes.</param>
		/// <returns>The number of minutes that the strings represent.</returns>
		public static int? GetMinutesFromValues(string Hours, string Minutes, bool AllowNull = true)
		{
			int? retInt = null;

			if (!string.IsNullOrWhiteSpace(Hours) || !string.IsNullOrWhiteSpace(Minutes) || !AllowNull)
			{
				int intHours = 0;
				int intMinutes = 0;
				if (!string.IsNullOrWhiteSpace(Hours) && Hours.All<char>(char.IsNumber))
				{
					intHours = int.Parse(Hours);
				}
				if (!string.IsNullOrWhiteSpace(Minutes) && Minutes.All<char>(char.IsNumber))
				{
					intMinutes = int.Parse(Minutes);
				}

				retInt = (intHours * 60) + intMinutes;
			}

			return retInt;
		}
	}
}
