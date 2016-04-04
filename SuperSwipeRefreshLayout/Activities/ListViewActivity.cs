using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SuperSwipeRefreshLayoutDemoApp.Views;

namespace SuperSwipeRefreshLayoutDemoApp.Activities
{
    [Activity(Label = "ListViewActivity")]
    public class ListViewActivity : Activity
    {
        private Views.SuperSwipeRefreshLayout swipeRefreshLayout;

        private ListView listView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Activity_ListView);

            listView = FindViewById<ListView>(Resource.Id.List_View);
            listView.Adapter = new ArrayAdapter<String>(this,Android.Resource.Layout.SimpleListItem1, GetData());

            swipeRefreshLayout = FindViewById<SuperSwipeRefreshLayout>(Resource.Id.Swipe_Refresh);

            swipeRefreshLayout
                .SetOnPullRefreshListener(new MyOnPullRefreshListener(this));
        }

        private class MyOnPullRefreshListener : SuperSwipeRefreshLayout.IOnPullRefreshListener
        {
            private ListViewActivity _context;

            public MyOnPullRefreshListener(ListViewActivity context)
            {
                _context = context;
            }

            void Views.SuperSwipeRefreshLayout.IOnPullRefreshListener.OnRefresh()
            {

                Action myAction = () =>
                {
                    _context.swipeRefreshLayout.SetRefreshing(false);
                };

                new Handler().PostDelayed(myAction, 2000);

            }

            void Views.SuperSwipeRefreshLayout.IOnPullRefreshListener.OnPullDistance(int distance)
            {
                System.Console.WriteLine("debug:distance = " + distance);
            }

            void Views.SuperSwipeRefreshLayout.IOnPullRefreshListener.OnPullEnable(bool enable)
            {

            }
        }

        private List<String> GetData()
        {
            List<String> data = new List<String>();
            for (int i = 0; i < 20; i++)
            {
                data.Add("item -- " + i);
            }
            return data;
        }
    }
}