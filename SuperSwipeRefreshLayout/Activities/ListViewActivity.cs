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
        private SuperSwipeRefreshLayout _swipeRefreshLayout;

        private ListView _listView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Activity_ListView);

            _listView = FindViewById<ListView>(Resource.Id.List_View);
            _listView.Adapter = new ArrayAdapter<string>(this,Android.Resource.Layout.SimpleListItem1, GetData());

            _swipeRefreshLayout = FindViewById<SuperSwipeRefreshLayout>(Resource.Id.Swipe_Refresh);

            _swipeRefreshLayout.SetOnPullRefreshListener(new MyOnPullRefreshListener(this));
        }

        private class MyOnPullRefreshListener : SuperSwipeRefreshLayout.IOnPullRefreshListener
        {
            private readonly ListViewActivity _context;

            public MyOnPullRefreshListener(ListViewActivity context)
            {
                _context = context;
            }

            void SuperSwipeRefreshLayout.IOnPullRefreshListener.OnRefresh()
            {

                Action myAction = () =>
                {
                    _context._swipeRefreshLayout.SetRefreshing(false);
                };

                new Handler().PostDelayed(myAction, 2000);

            }

            void SuperSwipeRefreshLayout.IOnPullRefreshListener.OnPullDistance(int distance)
            {
                 Console.WriteLine("debug:distance = " + distance);
            }

            void SuperSwipeRefreshLayout.IOnPullRefreshListener.OnPullEnable(bool enable)
            {

            }
        }

        private List<string> GetData()
        {
            List<string> data = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                data.Add("item -- " + i);
            }
            return data;
        }
    }
}