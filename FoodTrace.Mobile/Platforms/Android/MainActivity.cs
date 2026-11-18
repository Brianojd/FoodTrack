using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Color = Android.Graphics.Color;

namespace FoodTrace.Mobile
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window?.SetStatusBarColor(Color.White);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && Window is not null)
                Window.DecorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LightStatusBar;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O && Window is not null)
                Window.DecorView.SystemUiVisibility |= (StatusBarVisibility)SystemUiFlags.LightNavigationBar;
        }
    }
}
