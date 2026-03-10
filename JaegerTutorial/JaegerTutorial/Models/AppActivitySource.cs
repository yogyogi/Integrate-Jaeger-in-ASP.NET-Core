using System.Diagnostics;

namespace JaegerTutorial.Models
{
    public class AppActivitySource : IDisposable
    {
        public static readonly string ActivitySourceName = "AppCustomActivity";
        private readonly ActivitySource activitySource = new ActivitySource(ActivitySourceName);

        public Activity AppActivity(string activityName, ActivityKind kind = ActivityKind.Internal)
        {
            var activity = activitySource.StartActivity(activityName, kind);
            return activity;
        }

        public void Dispose()
        {
            activitySource.Dispose();
        }
    }
}
