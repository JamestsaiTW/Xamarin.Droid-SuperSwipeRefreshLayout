using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using SuperSwipeRefreshLayoutDemoApp.Adapters;
using SuperSwipeRefreshLayoutDemoApp.Views;


namespace SuperSwipeRefreshLayoutDemoApp.Activities
{
    [Activity(Label = "RecyclerViewActivity")]
    public class RecyclerViewActivity : Activity
    {
        readonly List<String> list = new List<String>();

        private RecyclerView recyclerView;
        private RecyclerAdapter myAdapter;
        private LinearLayoutManager linearLayoutManager;
        private Views.SuperSwipeRefreshLayout swipeRefreshLayout;

        // Header View
        private ProgressBar progressBar;
        private TextView textView;
        private ImageView imageView;

        // Footer View
        private ProgressBar footerProgressBar;
        private TextView footerTextView;
        private ImageView footerImageView;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Activity_RecyclerView);

            recyclerView = (RecyclerView)FindViewById(Resource.Id.recycler_view);
            linearLayoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(linearLayoutManager);
            myAdapter = new RecyclerAdapter(this);
            recyclerView.SetAdapter(myAdapter);


            swipeRefreshLayout = FindViewById<Views.SuperSwipeRefreshLayout>(Resource.Id.Swipe_Refresh);
            swipeRefreshLayout.SetHeaderView(CreateHeaderView());
            swipeRefreshLayout.SetFooterView(CreateFooterView());
            swipeRefreshLayout.SetTargetScrollWithLayout(true);

            swipeRefreshLayout.SetOnPullRefreshListener(new MyOnPullRefreshListener(this));
            swipeRefreshLayout.SetOnPushLoadMoreListener(new MyOnPushLoadMoreListener(this));

            InitsDatas();

        }

        private View CreateFooterView()
        {
            View footerView = LayoutInflater.From(swipeRefreshLayout.Context)
                    .Inflate(Resource.Layout.layout_footer, null);
            footerProgressBar = (ProgressBar)footerView
                    .FindViewById(Resource.Id.footer_pb_view);
            footerImageView = (ImageView)footerView
                    .FindViewById(Resource.Id.footer_image_view);
            footerTextView = (TextView)footerView
                    .FindViewById(Resource.Id.footer_text_view);
            footerProgressBar.Visibility = ViewStates.Gone;
            footerImageView.Visibility = ViewStates.Visible;
            footerImageView.SetImageResource(Resource.Drawable.down_arrow);
            footerTextView.Text = "資料載入中....";
            return footerView;
        }

        private View CreateHeaderView()
        {
            View headerView = LayoutInflater.From(swipeRefreshLayout.Context)
                    .Inflate(Resource.Layout.layout_head, null);
            progressBar = (ProgressBar)headerView.FindViewById(Resource.Id.pb_view);
            textView = (TextView)headerView.FindViewById(Resource.Id.text_view);
            textView.Text = "資料更新中...";
            imageView = (ImageView)headerView.FindViewById(Resource.Id.image_view);
            imageView.Visibility = ViewStates.Visible;
            imageView.SetImageResource(Resource.Drawable.down_arrow);
            progressBar.Visibility = ViewStates.Gone;
            return headerView;
        }

        private void InitsDatas()
        {
            for (int i = 1; i <= 50; i++)
            {
                list.Add("item " + (list.Count + 1));
            }
            myAdapter.AddAll(list, 0);
        }

        private class MyOnPullRefreshListener : SuperSwipeRefreshLayout.IOnPullRefreshListener
        {
            private RecyclerViewActivity _context;

            public MyOnPullRefreshListener(RecyclerViewActivity context)
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

        private class MyOnPushLoadMoreListener : SuperSwipeRefreshLayout.IOnPushLoadMoreListener
        {
            private RecyclerViewActivity _context;

            public MyOnPushLoadMoreListener(RecyclerViewActivity context)
            {
                _context = context;
            }

            void SuperSwipeRefreshLayout.IOnPushLoadMoreListener.OnLoadMore()
            {
                _context.footerTextView.Text = "讀取更多資料中...";
                _context.footerImageView.Visibility = ViewStates.Gone;
                _context.footerProgressBar.Visibility = ViewStates.Visible;

                Action myAction = () =>
                {

                    _context.footerImageView.Visibility = ViewStates.Visible;
                    _context.progressBar.Visibility = ViewStates.Gone;
                    _context.swipeRefreshLayout.SetLoadMore(false);
                };

                new Handler().PostDelayed(myAction, 5000);
            }

            void SuperSwipeRefreshLayout.IOnPushLoadMoreListener.OnPushDistance(int distance)
            {

            }

            void SuperSwipeRefreshLayout.IOnPushLoadMoreListener.OnPushEnable(bool enable)
            {
                _context.footerTextView.Text = enable ? "放開 讀取更多資料" : "上拉 讀取更多資料";
                _context.footerImageView.Visibility = ViewStates.Visible;
                _context.footerImageView.Rotation = enable ? 0 : 180;
            }
        }
    }
}