using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using TCX.Configuration;
using TCX.PBXAPI;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace WebAPI {
  class Program {

    static Dictionary <string, Dictionary <string, string>> iniContent = new Dictionary <string, Dictionary <string, string>> (StringComparer.InvariantCultureIgnoreCase);

    class ProgramOptions
    {
        public string Port { get; set; } = string.Empty;
        public bool DebugMode { get; set; } = false;
        public string SsoUri { get; set; } = string.Empty;
    }

    public static bool Stop {
      get;
      private set;
    }
    public static String Debugger;
    static void ReadConfiguration(string filePath) {
      var content = File.ReadAllLines(filePath);
      Dictionary <string, string> CurrentSection = null;
      string CurrentSectionName = null;
      for (int i = 1; i < content.Length + 1; i++) {
        var s = content[i - 1].Trim();
        if (s.StartsWith("[")) {
          CurrentSectionName = s.Split(new [] {
            '[',
            ']'
          }, StringSplitOptions.RemoveEmptyEntries)[0];
          CurrentSection = iniContent[CurrentSectionName] = new Dictionary <string, string> (StringComparer.InvariantCultureIgnoreCase);
        } else if (CurrentSection != null && !string.IsNullOrWhiteSpace(s) && !s.StartsWith("#") && !s.StartsWith(";")) {
          var res = s.Split("=").Select(x => x.Trim()).ToArray();
          CurrentSection[res[0]] = res[1];
        } else {
          //Logger.WriteLine($"Ignore Line {i} in section '{CurrentSectionName}': '{s}' ");
        }
      }
      instanceBinPath = Path.Combine(iniContent["General"]["AppPath"], "Bin");
    }

    static async Task Bootstrap(string[] args) {
      PhoneSystem.CfgServerHost = "127.0.0.1";
      PhoneSystem.CfgServerPort = int.Parse(iniContent["ConfService"]["ConfPort"]);
      PhoneSystem.CfgServerUser = iniContent["ConfService"]["confUser"];
      PhoneSystem.CfgServerPassword = iniContent["ConfService"]["confPass"];
      var ps = PhoneSystem.Reset(
        PhoneSystem.ApplicationName + new Random(Environment.TickCount).Next().ToString(), "127.0.0.1", int.Parse(iniContent["ConfService"]["ConfPort"]), iniContent["ConfService"]["confUser"], iniContent["ConfService"]["confPass"]);
      ps.WaitForConnect(TimeSpan.FromSeconds(3));
      try {
        if (!HttpListener.IsSupported) {
          Logger.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
          return;
        }

        ProgramOptions options = ParseArguments(args);
        
        var prefixes = new List <string> () {
          $"http://127.0.0.1:{options.Port}/"
        };
        if (options.DebugMode) {
          Program.Debugger = "debug";
        }
        AuthTokenValidatorSso tokenValidator = new AuthTokenValidatorSso(options.SsoUri);

        // Create a listener.
        HttpListener listener = new HttpListener();
        // Add the prefixes.
        foreach(string s in prefixes) {
          Logger.WriteLine(s);
          listener.Prefixes.Add(s);
        }
        listener.Start();

        Logger.WriteLine("Listening... on Port " + options.Port + " for Development");
        Logger.WriteLine("To Stop open URL: http://127.0.0.1:" + options.Port + "/stop");
        Boolean notExit = true;
        while (notExit) {
          HttpListenerContext context = listener.GetContext();
          HttpListenerRequest request = context.Request;
          HttpListenerResponse response = context.Response;

          Logger.WriteLine(request.RawUrl);

          if (request.HttpMethod == "OPTIONS") {
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, Content-Length, X-Requested-With, x-request-id");
            response.StatusCode = (int) HttpStatusCode.OK;
            response.OutputStream.Close();
            continue;
          }

          string token = request.Headers["Authorization"]?.Replace("Bearer ", "");
          if (string.IsNullOrEmpty(token))
          {
            Logger.WriteLine("No token provided");
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response.OutputStream.Close();
            continue;
          }

          bool isValid = await tokenValidator.Validate(token);
          if (!isValid)
          {
            Logger.WriteLine("Invalid token");
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response.OutputStream.Close();
            continue;
          }

          string documentContents;
          using(Stream receiveStream = request.InputStream) {
            using(StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8)) {
              documentContents = readStream.ReadToEnd();
            }
          }
          String url = request.RawUrl;
          String[] queryStringArray = url.Split('/');
          try {

            string connectionAsString(ActiveConnection ac) {
              return $"ID={ac.ID}:CCID={ac.CallConnectionID}:S={ac.Status}:DN={ac.DN.Number}:EP={ac.ExternalParty}:REC={ac.RecordingState}";
            }
            string respval = "idle";
            string text;
            using(var reader = new StreamReader(request.InputStream, request.ContentEncoding)) {
              text = reader.ReadToEnd();
            }

            switch (queryStringArray[1]) {
              case "showallcalls":
                {
                  respval = getcall.showallcall();
                }
                break;
              case "makecall":
                {
                  respval = makedirectcall.dial(queryStringArray[2], queryStringArray[3], queryStringArray[4]);
                  //Logger.WriteLine("call to " + queryStringArray[3]);
                }
                break;
              case "ready":
                {
                  string callerid = profiles.setstatus(queryStringArray[2], "avail");
                  string status = "";
                  if (callerid == "Available") {
                    status = queuecontroll.status("login_all", queryStringArray[2]);
                  } else {
                    status = "false";
                  }
                  respval = status;
                }
                break;
              case "notready":
                {
                  string callerid = profiles.setstatus(queryStringArray[2], "custom1");
                  string status = "";
                  if (callerid == "Custom 1") {
                    status = queuecontroll.status("logout_all", queryStringArray[2]);
                  } else {
                    status = "false";
                  }
                  respval = status;
                }
                break;
              case "logout":
                {
                  string callerid = profiles.setstatus(queryStringArray[2], "away");
                  string status = "";
                  if (callerid == "Away") {
                    status = queuecontroll.status("logout_all", queryStringArray[2]);
                  } else {
                    status = "false";
                  }
                  respval = status;
                }
                break;
              case "login":
                {
                  respval = queuecontroll.status(queryStringArray[2], queryStringArray[3]);
                }
                break;
              case "dnregs":
                {
                  respval = getdnregs.status(queryStringArray[2]);
                }
                break;
              case "ondn":
                {
                  respval = getcallid.showcallid(queryStringArray[2]);
                }
                break;
              case "getcallerid":
                {
                  respval = getcallqueuenumber.showid(queryStringArray[2]);
                }
                break;
              case "drop":
                {
                  respval = dropcall.dropcallid(queryStringArray[2]);
                }
                break;
              case "answer":
                {
                  string wert = answer.call(queryStringArray[2]);
                  string mod2 = "";
                  Logger.WriteLine("Wert:" + wert);
                  if (wert == "true") {
                    mod2 = getcallid.showcallid(queryStringArray[2]);
                  } else {
                    mod2 = "null";
                  }
                  respval = mod2;
                }
                break;
              case "record":
                {
                  string mod2 = "";
                  string mod3 = "";
                  using(var dn = PhoneSystem.Root.GetDNByNumber(queryStringArray[2])) {
                    using(var connections = dn.GetActiveConnections().GetDisposer()) {
                      var alltakenconnections = connections.ToDictionary(x => x, y => y.OtherCallParties);
                      foreach(var kv in alltakenconnections) {
                        var owner = kv.Key;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Logger.WriteLine($"Call {kv.Key.CallID}:");
                        Console.ResetColor();
                        string result = connectionAsString(owner);
                        Logger.WriteLine(result);
                        if (result.Contains("S=Connected")) {
                          int cut = result.IndexOf(':');
                          string mod = result.Substring(0, cut);
                          mod2 = mod.Substring(3);
                          Console.ForegroundColor = ConsoleColor.Red;
                          Logger.WriteLine("Active Connection Number is:");
                          Logger.WriteLine(mod2);
                          Console.ResetColor();
                        } else {
                          Console.ForegroundColor = ConsoleColor.Red;
                          Logger.WriteLine("Active Connection Number is unknown");
                          Console.ResetColor();
                        }
                      }
                    }
                  }
                  int i = 0;
                  try {
                    i = System.Convert.ToInt32(mod2);
                  } catch(FormatException) {
                    // the FormatException is thrown when the string text does 
                    // not represent a valid integer.
                  } catch(OverflowException) {
                    // the OverflowException is thrown when the string is a valid integer, 
                    // but is too large for a 32 bit integer.  Use Convert.ToInt64 in
                    // this case.
                  }
                  Logger.WriteLine("Record on Extension:" + i);
                  if (i > 0) {
                    mod3 = "true";
                    if (Enum.TryParse(queryStringArray[3], out RecordingAction ra)) PhoneSystem.Root.GetByID <ActiveConnection> (i).ChangeRecordingState(ra);
                    else throw new ArgumentOutOfRangeException("Invalid record action");
                  } else {
                    mod3 = "false";
                  }
                  respval = mod3;
                }
                break;
              case "transfer":
                {
                  respval = transfercall.cold(queryStringArray[2], queryStringArray[3]);
                }
                break;
              case "park":
                {
                  respval = park.call(queryStringArray[2]);
                }
                break;
              case "unpark":
                {
                  string arg2 = "*10";
                  respval = makedirectcall.dial(queryStringArray[2], arg2, queryStringArray[3]);

                }
                break;
              case "atttrans":
                {
                  respval = atttrans.dotrans(queryStringArray[2]);
                }
                break;
              case "setstatus":
                {
                  respval = profiles.setstatus(queryStringArray[2], queryStringArray[3]);
                }
                break;
              case "save":
                {
                  respval = savechanges.Run(queryStringArray[2], queryStringArray[3], queryStringArray[4]);
                }
                break;
              case "showstatus":
                {
                  respval = profiles.show(queryStringArray[2]);
                }
                break;
              case "divert":
                {

                }
                break;
              case "clear":
                {
                  Console.WriteLine("Clearing the screen!");
                  Console.Clear();
                }
                break;
              case "stop":
                {
                  respval = "<HTML><BODY> Server Stopped</BODY></HTML>";
                  notExit = false;
                  break;
                  throw new Exception("System Stopped");
                }
              default:
                break;
            }
            // Obtain a response object.
            Logger.WriteLine(respval);
            // Allow cross origin requests
            response.AddHeader("Access-Control-Allow-Origin", "*");
            // Construct a response.
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(respval);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
          } catch(Exception ex) {
            if (queryStringArray[1] == "stop") {
              Logger.WriteLine("system Stopped");
              throw new Exception("System Stopped");
            } else {
              Logger.WriteLine(ex.Message);
              response.StatusCode = 500;
              // Allow cross origin requests
              response.AddHeader("Access-Control-Allow-Origin", "*");
              response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
              response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, Content-Length, X-Requested-With, x-request-id");
              byte[] buffer = System.Text.Encoding.UTF8.GetBytes(ex.Message);
              response.ContentLength64 = buffer.Length;
              System.IO.Stream output = response.OutputStream;
              output.Write(buffer, 0, buffer.Length);
              continue;
            }
          } finally {
            response.OutputStream.Close();
          }
        }
        listener.Stop();
      } finally {
        ps.Disconnect();
      }
    }

    static string instanceBinPath;

    static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
      var name = new AssemblyName(args.Name).Name;
      try {
        return Assembly.LoadFrom(Path.Combine(instanceBinPath, name + ".dll"));
      } catch {
        return null;
      }
    }

    static void Main(string[] args) {
        MainAsync(args).GetAwaiter().GetResult();
    }

    static async Task MainAsync(string[] args) {
        try {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3CXPhoneSystem.ini");
            if (!File.Exists(filePath)) {
                Logger.WriteLine(filePath);
                throw new Exception("Cannot find 3CXPhoneSystem.ini");
            }
            ReadConfiguration(filePath);
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            await Bootstrap(args);
            Logger.WriteLine("exited gracefully");
        } catch(Exception ex) {
            Logger.WriteLine(ex.ToString());
        }
    }

    static ProgramOptions ParseArguments(string[] args)
    {
        ProgramOptions options = new ProgramOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-p":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedPort))
                    {
                        options.Port = args[i + 1];
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("Invalid or missing port number");
                    }
                    break;

                case "-l":
                    if (i + 1 < args.Length && args[i + 1].Equals("debug", StringComparison.OrdinalIgnoreCase))
                    {
                        options.DebugMode = true;
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("Expected 'debug' after -l");
                    }
                    break;

                case "-u":
                    if (i + 1 < args.Length)
                    {
                        options.SsoUri = args[i + 1];
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("Missing URI after -u");
                    }
                    break;

                default:
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    break;
            }
        }

        if (options.Port == string.Empty) {
          Logger.WriteLine("No port submitted, use generic port: 8889");
          options.Port = "8889";
        } 
        else 
        {
          Logger.WriteLine($"Port: {options.Port}");
        }

        if (options.DebugMode) Logger.WriteLine("Debug mode ON");
        else Logger.WriteLine("Debug mode OFF");
        
        if (options.SsoUri == string.Empty) {
          Logger.WriteLine("No SSO Uri submitted, use generic uri: http://localhost:8080");
          options.SsoUri = "http://localhost:8080";
        }
        else
        {
          Logger.WriteLine($"SSO Uri: {options.SsoUri}");
        }

        return options;
    }

  }

}