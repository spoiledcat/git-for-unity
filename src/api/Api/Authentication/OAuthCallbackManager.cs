using System.Collections.Specialized;
using System.Linq;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
#if NET35
using System.Web;
#endif
using Unity.Git;

namespace Unity.Git
{
    public interface IOAuthCallbackManager
    {
        event Action<string, string> OnCallback;
        bool IsRunning { get; }
        void Start();
        void Stop();
    }

    public class OAuthCallbackManager : IOAuthCallbackManager
    {
        const int CallbackPort = 42424;
        public static readonly Uri CallbackUrl = new Uri($"http://localhost:{CallbackPort}/callback");

        private static readonly ILogging logger = LogHelper.GetLogger<OAuthCallbackManager>();
        private static readonly object _lock = new object();


        private readonly CancellationTokenSource cancelSource;

        private HttpListener httpListener;
        public bool IsRunning { get; private set; }

        public event Action<string, string> OnCallback;

        public OAuthCallbackManager()
        {
            cancelSource = new CancellationTokenSource();
        }

        public void Start()
        {
            if (!IsRunning)
            {
                lock(_lock)
                {
                    if (!IsRunning)
                    {
                        logger.Trace("Starting");

                        httpListener = new HttpListener();
                        httpListener.Prefixes.Add(CallbackUrl.AbsoluteUri + "/");
                        httpListener.Start();
                        Task.Factory.StartNew(Listen, cancelSource.Token);
                        IsRunning = true;
                    }
                }
            }
        }

        public void Stop()
        {
            logger.Trace("Stopping");
            cancelSource.Cancel();
        }

        private void Listen()
        {
            try
            {
                using (httpListener)
                {
                    using (cancelSource.Token.Register(httpListener.Stop))
                    {
                        while (true)
                        {
                            var context = httpListener.GetContext();
#if NET35
                            var queryParts = HttpUtility.ParseQueryString(context.Request.Url.Query);
#else

                            var queryParts = new NameValueCollection();
                            context.Request.Url.Query.Split('&').All(x => {
                                var parts = x.Split('=');
                                queryParts.Add(WebUtility.UrlDecode(parts[0]), WebUtility.UrlDecode(parts[1]));
                                return true;
                            });
#endif
                            var state = queryParts["state"];
                            var code = queryParts["code"];

                            logger.Trace("OnCallback: {0}", state);
                            if (OnCallback != null)
                            {
                                OnCallback(state, code);
                            }

                            context.Response.StatusCode = 200;
                            context.Response.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Trace(ex.Message);
            }
            finally
            {
                IsRunning = false;
                httpListener = null;
            }
        }
    }
}
