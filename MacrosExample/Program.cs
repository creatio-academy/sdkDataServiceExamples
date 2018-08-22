using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Terrasoft.Common;
using Terrasoft.Core.Entities;
using Terrasoft.Nui.ServiceModel.DataContract;
namespace MacrosExample
{
    // The helper class. Used to convert authentication response JSON string.
    class AuthResponseStatus
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object Exception { get; set; }
        public object PasswordChangeUrl { get; set; }
        public object RedirectUrl { get; set; }
    }

    class Program
    {
        // Main bpm'online URL. Has to be changed to a custom one, for example, http://myapplication.bpmonline.com.
        private const string baseUri = "http://localhost/bpmonline7.12.3";

        // Container for cookie authentication in bpm'online. Must be used in subsequent requests.
        public static CookieContainer AuthCookie = new CookieContainer();

        // Query string to the Login method of the AuthService.svc service.
        private const string authServiceUri = baseUri + @"/ServiceModel/AuthService.svc/Login";

        // SelectQuery query path string.
        private const string selectQueryUri = baseUri + @"/0/DataService/json/SyncReply/SelectQuery";

        /// <summary>
        /// Performs user authentication request.
        /// </summary>
        /// <param name="userName">The name of the bpm'online user.</param>
        /// <param name="userPassword">The passord of the bpm'online user.</param>
        /// <returns></returns>
        public static bool TryLogin(string userName, string userPassword)
        {
            // Creating an instance of the authentication service request.
            var authRequest = HttpWebRequest.Create(authServiceUri) as HttpWebRequest;

            // Specifying the request HTTP method.
            authRequest.Method = "POST";

            // Defining the request's content type.
            authRequest.ContentType = "application/json";

            // Enabling the use of cookies in the request.
            authRequest.CookieContainer = AuthCookie;

            // Placing user credentials to the request.
            using (var requestStream = authRequest.GetRequestStream())
            {
                using (var writer = new StreamWriter(requestStream))
                {
                    writer.Write(@"{
                    ""UserName"":""" + userName + @""",
                    ""UserPassword"":""" + userPassword + @"""
                    }");
                }
            }

            // Auxiliary object where the HTTP reply data will be de-serialized.
            AuthResponseStatus status = null;

            // Getting a reply from the server. If the authentication is successful, cookie will be placed to the AuthCookie property.
            // These cookies must be used for subsequent requests.
            using (var response = (HttpWebResponse)authRequest.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    // De-serialization of the HTTP response to an auxiliary object.
                    string responseText = reader.ReadToEnd();
                    status = new JavaScriptSerializer().Deserialize<AuthResponseStatus>(responseText);
                }

            }

            // Checking authentication status.
            if (status != null)
            {
                // Authentication is successful.
                if (status.Code == 0)
                {
                    return true;
                }
                // Authentication is unsuccessful.
                Console.WriteLine(status.Message);
            }
            return false;
        }

        // Main method of the application.
        static void Main(string[] args)
        {
            // Calling authentication method. If the user is not authenticated, the application exits.
            if (!TryLogin("Supervisor", "Supervisor"))
            {
                Console.WriteLine("Authentication error!");
                return;
            }

            // Instance of the SelectQuery class.
            var selectQuery = new SelectQuery()
            {
                /// Root schema name.
                RootSchemaName = "Contact",
                // Adding column values collection.
                Columns = new SelectQueryColumns()
                {
                    // Columns key-value pairs collection.
                    Items = new Dictionary<string, SelectQueryColumn>()
                    {
                        //Column [Name].
                        {
                            // Key.
                            "Name",
                            // Value.
                            new SelectQueryColumn()
                            {
                                // Expression class instance of the entity schema query.
                                // Used to configure the [Full name] column.    
                                Expression = new ColumnExpression()
                                {
                                    // Entity schema query expression type is a schema column.
                                    ExpressionType = EntitySchemaQueryExpressionType.SchemaColumn,
                                    // Path to column.
                                    ColumnPath = "Name"
                                }
                            }
                        },
                        // Configuring the [Number of activities] column.
                       {
                            "BirthDate",
                            new SelectQueryColumn()
                            {
                                Expression = new ColumnExpression()
                                {
                                    ExpressionType = EntitySchemaQueryExpressionType.SchemaColumn,
                                    ColumnPath = "BirthDate"
                                }
                            }
                        }
                    }
                }
            };
            // Query filters.
            var selectFilters = new Filters()
            {
                // Query filters.
                FilterType = Terrasoft.Nui.ServiceModel.DataContract.FilterType.FilterGroup,
                // Fiters key-value pair collection.
                Items = new Dictionary<string, Filter>
                {
                    
                    // Filtering birth year. It must be eaual 1986.
                    {
                        // Key.
                        "FilterYear",
                        // Value.
                        new Filter
                        {
                            // Filter type is comparison.
                            FilterType = Terrasoft.Nui.ServiceModel.DataContract.FilterType.CompareFilter,
                            // Comparison type is equality.
                            ComparisonType = FilterComparisonType.Equal,
                            // Expression to check.
                            LeftExpression = new BaseExpression()
                            {
                                // Expression type is schema column.
                                ExpressionType = EntitySchemaQueryExpressionType.SchemaColumn,
                                // Path to schema.
                                ColumnPath = "BirthDate"
                            },
                            // Expression with which the checked value is compared.
                            RightExpression = new BaseExpression
                            {
                                // Expression type is function.
                                ExpressionType = EntitySchemaQueryExpressionType.Function,
                                // Function type is macro.
                                FunctionType = FunctionType.Macros,
                                // Macro type is year.
                                MacrosType = EntitySchemaQueryMacrosType.Year,
                                // Function argument.
                                FunctionArgument = new BaseExpression
                                {
                                    // Type of expression that determines the argument is parameter.
                                    ExpressionType = EntitySchemaQueryExpressionType.Parameter,
                                    // Parameter initialization.
                                    Parameter = new Parameter
                                    {
                                        // Parameter type — integer.
                                        DataValueType = DataValueType.Integer,
                                        // Parameter value.
                                        Value = "1986"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Adding filters to the query.
            selectQuery.Filters = selectFilters;
            // Serializing the SelectQuery instance to a JSON string.
            var json = new JavaScriptSerializer().Serialize(selectQuery);
            // You can log the JSON string to the console.
            Console.WriteLine(json);
            Console.WriteLine();

            // Converting JSON string to a byte array.
            byte[] jsonArray = Encoding.UTF8.GetBytes(json);
            // Creating HTTP request instance.
            var selectRequest = HttpWebRequest.Create(selectQueryUri) as HttpWebRequest;
            // Defining HTTP method of the request.
            selectRequest.Method = "POST";
            // Defining request content type.
            selectRequest.ContentType = "application/json";
            // Adding the previously received authentication cookies.
            selectRequest.CookieContainer = AuthCookie;
            // Setting the request content length.
            selectRequest.ContentLength = jsonArray.Length;

            // Putting BPMCSRF token to the request header.
            CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
            string csrfToken = cookieCollection["BPMCSRF"].Value;
            selectRequest.Headers.Add("BPMCSRF", csrfToken);

            // Adding a JSON string to the request body.
            using (var requestStream = selectRequest.GetRequestStream())
            {
                requestStream.Write(jsonArray, 0, jsonArray.Length);
            }

            // Executing the HTTP request and receiving response from the server.
            using (var response = (HttpWebResponse)selectRequest.GetResponse())
            {
                // Displaying respose in console.
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    Console.WriteLine(reader.ReadToEnd());
                    // Response is just a JSON string.
                    // The main prooperties of such JSON are:
                    // "rowConfig" - contains the structure of response records.
                    // "rows" - contains the collection of response records.
                    // "success" - indicates whether the record was added successfully.
                    // You can convert this JSON to a plain old CLR object in same way as it is done in TryLogin() method above.
                    // But you have to define corresponding class before doing this.
                }
            }

            // Pause.
            Console.ReadKey();

        }
    }
}
