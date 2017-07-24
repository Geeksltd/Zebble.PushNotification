namespace Zebble.Plugin
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json.Linq;
    using System.Threading.Tasks;
    using Android.Content;
    using Firebase.Iid;
    using Firebase.Messaging;
    using Android.App;
    using Java.IO;
    using System.Collections.Generic;
    using Zebble;
    using Zebble.NativeImpl;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PushNotification : DevicePushNotification.INativeImplementation
    {
        static string SenderId => Framework.Config.Get("Push.Notification.Android.Sender.ID");
        static Context Context => UIRuntime.CurrentActivity;

        public async Task DoRegister()
        {
            try
            {
                Firebase.FirebaseApp.InitializeApp(Context);
                var token = FirebaseInstanceId.Instance.Token;
                await Device.PushNotification.Registered.RaiseOn(Device.ThreadPool, token);
            }
            catch (Exception ex)
            {
                throw new Exception("Push-Notification registeration failed: " + ex);
            }
        }

        public Task DoUnregister() => Device.ThreadPool.Run(DoUnregisterOnThreadPool);

        async Task DoUnregisterOnThreadPool()
        {
            try
            {
                FirebaseInstanceId.Instance.DeleteToken(SenderId, FirebaseMessaging.InstanceIdScope);
                await Device.PushNotification.UnRegistered.RaiseOn(Device.ThreadPool);
            }
            catch (IOException ex)
            {
                await Device.PushNotification.ReceivedError.RaiseOn(Device.ThreadPool, "Failed to unregister Push Notification: " + ex);
            }
        }

        [Service(Exported = false)]
        [IntentFilter(new string[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
        public class RefreshService : FirebaseInstanceIdService
        {
            // Called if InstanceID token is updated. This may occur if the security of the previous token had been compromised. This call is initiated by the InstanceID provider.
            public override void OnTokenRefresh()
            {
                base.OnTokenRefresh();

                Device.ThreadPool.RunAction(async () =>
                {
                    try
                    {
                        var token = FirebaseInstanceId.Instance.Token;
                        await Device.PushNotification.Registered.RaiseOn(Device.ThreadPool, token);
                        Device.Log.Message("Refreshed token: " + token);
                    }
                    catch (Exception ex)
                    {
                        await Device.PushNotification.ReceivedError.RaiseOn(Device.ThreadPool,
                            "Failed to refresh the installation ID: " + ex);
                    }
                });
            }
        }

        [Service]
        [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
        public class PushNotificationGcmListener : FirebaseMessagingService
        {
            static Context Context => UIRuntime.CurrentActivity;
            public override void OnMessageReceived(RemoteMessage message)
            {
                var parameters = new Dictionary<string, object>();
                var values = new JObject();
                foreach (var data in message.Data)
                {
                    try { values.Add(data.Key, JObject.Parse(data.Value)); }
                    catch { values.Add(data.Key, data.Value); }

                    parameters.Add(data.Key, data);
                }

                var notification = new NotificationMessage(values);
                Device.PushNotification.ReceivedMessage.RaiseOn(Device.ThreadPool, notification);
            }
        }

        public async Task<bool> OnMessageReceived(object message) { return true; }

        public async Task OnRegisteredSuccess(object token) { }

        public async Task OnUnregisteredSuccess() { }
    }
}