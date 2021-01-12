namespace Zebble.Device
{
    using Olive;
    using System;
    using System.Threading.Tasks;
    using static Zebble.Device.LocalNotification;

    public static partial class PushNotification
    {
        /// <summary>The event parameter contains the values.</summary>
        public static readonly AsyncEvent<Notification> ReceivedMessage = new AsyncEvent<Notification>();

        /// <summary>
        /// When first an installed app registers to receive push notifications,
        /// it will contact the Apple, Google or Microsoft Push Notification service to receive a token.
        /// Then this event (Registered) will be fired.
        /// You should handle this method in your app code to call your own service API,
        /// and register the app installation token with the user record (whatever it is in your domain).
        /// NOTE: Make sure to send the platform name as well, so that when later on you're trying to send push notifications to this user, you know which push notification service to send it to.
        /// </summary>
        public static readonly AsyncEvent<string> Registered = new AsyncEvent<string>();

        /// <summary>
        /// This event is fired when the user opts out of receiving push notifications.
        /// You should invoke your service API to remove the push notification token
        /// from the app user record (whatever it is in your domain) for this device.
        /// </summary>
        public static readonly AsyncEvent UnRegistered = new AsyncEvent();

        /// <summary>This event is fired when something goes wrong.
        /// The event parameter is the error message.</summary>
        public static readonly AsyncEvent<string> ReceivedError = new AsyncEvent<string>();

        static PushNotification()
        {
            ReceivedError.Handle(error => Log.For(typeof(PushNotification)).Error("Push Notification Error: " + error));
            Init();
        }

        public static async Task Register(OnError errorAction = OnError.Toast)
        {
            try { await Thread.UI.Run(DoRegister); }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to register for push notification.");
            }
        }

        public static async Task UnRegister(OnError errorAction = OnError.Toast)
        {
            try { await Thread.UI.Run(DoUnRegister); }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to un-register from push notification.");
            }
        }

        public static async Task<bool> OnMessageReceived(object message, OnError errorAction = OnError.Toast)
        {
            try { return await OnMessageReceived(message); }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to process the received message.");
                return false;
            }
        }

        public static async Task OnRegisteredSuccess(object token, OnError errorAction = OnError.Toast)
        {
            try { await OnRegisteredSuccess(token); }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to consume the token provided by the push notification server.");
            }
        }
    }
}