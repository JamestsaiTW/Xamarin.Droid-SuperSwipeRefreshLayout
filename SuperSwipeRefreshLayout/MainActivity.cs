using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using SuperSwipeRefreshLayoutDemoApp.Activities;

namespace SuperSwipeRefreshLayoutDemoApp
{
    [Activity(Label = "SuperSwipeRefreshLayout", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            var recyclerViewButton = FindViewById<Button>(Resource.Id.RecyclerViewButton);

            recyclerViewButton.Click += (sender, args) =>
            {
                StartActivity(typeof(RecyclerViewActivity));
            };

            var listViewButton = FindViewById<Button>(Resource.Id.ListViewButton);

            listViewButton.Click += (sender, args) =>
            {
                StartActivity(typeof(ListViewActivity));
            };

            var scrollViewButton = FindViewById<Button>(Resource.Id.ScrollViewButton);

            scrollViewButton.Click += (sender, args) =>
            {
                StartActivity(typeof(ScrollViewActivity));
            };
        }
    }
}

