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
        private SuperSwipeRefreshLayout _swipeRefreshLayout;

        private ProgressBar _progressBar;

        private TextView _textView;

        private ImageView _imageView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Activity_ScrollView);
            _swipeRefreshLayout = (SuperSwipeRefreshLayout) FindViewById(Resource.Id.Swipe_Refresh);
            View child = LayoutInflater.From(_swipeRefreshLayout.Context).Inflate(Resource.Layout.layout_head, null);

            _progressBar = (ProgressBar) child.FindViewById(Resource.Id.pb_view);
            _textView = (TextView) child.FindViewById(Resource.Id.text_view);
            _textView.Text = "下拉更新";
            _imageView = (ImageView) child.FindViewById(Resource.Id.image_view);
            _imageView.Visibility = ViewStates.Visible;
            _imageView.SetImageResource(Resource.Drawable.down_arrow);
            _progressBar.Visibility = ViewStates.Gone;
            _swipeRefreshLayout.SetHeaderView(child);


            _swipeRefreshLayout.SetOnPullRefreshListener(new MyOnPullRefreshListener(this));
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
                _context._textView.Text = "資料更新中...";
                _context._imageView.Visibility = ViewStates.Gone;
                _context._progressBar.Visibility = ViewStates.Visible;

                Action myAction = () =>
                {
                    _context._swipeRefreshLayout.SetRefreshing(false);
                    _context._progressBar.Visibility = ViewStates.Gone;

                };

                new Handler().PostDelayed(myAction, 2000);

            }

            void SuperSwipeRefreshLayout.IOnPullRefreshListener.OnPullDistance(int distance)
            {

            }

            void SuperSwipeRefreshLayout.IOnPullRefreshListener.OnPullEnable(bool enable)
            {
                _context._textView.Text = enable ? "放開 更新資料" : "再下拉 更新資料";
                _context._imageView.Visibility = ViewStates.Visible;
                _context._imageView.Rotation = enable ? 180 : 0;
            }
        }
    }
}