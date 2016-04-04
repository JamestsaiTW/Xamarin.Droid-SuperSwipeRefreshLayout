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
    [Activity(Label = "ScrollViewActivity")]
    public class ScrollViewActivity : Activity
    {
        private SuperSwipeRefreshLayout swipeRefreshLayout;

        private ProgressBar progressBar;

        private TextView textView;

        private ImageView imageView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Activity_ScrollView);
            swipeRefreshLayout = (SuperSwipeRefreshLayout) FindViewById(Resource.Id.Swipe_Refresh);
            View child = LayoutInflater.From(swipeRefreshLayout.Context).Inflate(Resource.Layout.layout_head, null);

            progressBar = (ProgressBar) child.FindViewById(Resource.Id.pb_view);
            textView = (TextView) child.FindViewById(Resource.Id.text_view);
            textView.Text = "下拉更新";
            imageView = (ImageView) child.FindViewById(Resource.Id.image_view);
            imageView.Visibility = ViewStates.Visible;
            imageView.SetImageResource(Resource.Drawable.down_arrow);
            progressBar.Visibility = ViewStates.Gone;
            swipeRefreshLayout.SetHeaderView(child);


            swipeRefreshLayout.SetOnPullRefreshListener(new MyOnPullRefreshListener(this));
        }

        private class MyOnPullRefreshListener : SuperSwipeRefreshLayout.IOnPullRefreshListener
        {
            private ScrollViewActivity _context;

            public MyOnPullRefreshListener(ScrollViewActivity context)
            {
                _context = context;
            }

            void SuperSwipeRefreshLayout.IOnPullRefreshListener.OnRefresh()
            {
                _context.textView.Text = "資料更新中...";
                _context.imageView.Visibility = ViewStates.Gone;
                _context.progressBar.Visibility = ViewStates.Visible;

                Action myAction = () =>
                {
                    _context.swipeRefreshLayout.SetRefreshing(false);
                    _context.progressBar.Visibility = ViewStates.Gone;

                };

                new Handler().PostDelayed(myAction, 2000);

            }

            void SuperSwipeRefreshLayout.IOnPullRefreshListener.OnPullDistance(int distance)
            {

            }

            void SuperSwipeRefreshLayout.IOnPullRefreshListener.OnPullEnable(bool enable)
            {
                _context.textView.Text = enable ? "放開 更新資料" : "再下拉 更新資料";
                _context.imageView.Visibility = ViewStates.Visible;
                _context.imageView.Rotation = enable ? 180 : 0;
            }
        }
    }
}