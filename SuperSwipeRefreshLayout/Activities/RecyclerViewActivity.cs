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
        readonly List<String> _dataList = new List<string>();

        private RecyclerView _recyclerView;
        private RecyclerAdapter _myRecyclerAdapter;
        private LinearLayoutManager _linearLayoutManager;
        private SuperSwipeRefreshLayout _swipeRefreshLayout;

        // Header View
        private ProgressBar _progressBar;
        private TextView _textView;
        private ImageView _imageView;

        // Footer View
        private ProgressBar _footerProgressBar;
        private TextView _footerTextView;
        private ImageView _footerImageView;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Activity_RecyclerView);

            _recyclerView = (RecyclerView)FindViewById(Resource.Id.recycler_view);
            _linearLayoutManager = new LinearLayoutManager(this);
            _recyclerView.SetLayoutManager(_linearLayoutManager);
            _myRecyclerAdapter = new RecyclerAdapter(this , _dataList );
            _recyclerView.SetAdapter(_myRecyclerAdapter);


            _swipeRefreshLayout = FindViewById<Views.SuperSwipeRefreshLayout>(Resource.Id.Swipe_Refresh);
            _swipeRefreshLayout.SetHeaderView(CreateHeaderView());
            _swipeRefreshLayout.SetFooterView(CreateFooterView());
            _swipeRefreshLayout.SetTargetScrollWithLayout(true);

            _swipeRefreshLayout.SetOnPullRefreshListener(new MyOnPullRefreshListener(this));
            _swipeRefreshLayout.SetOnPushLoadMoreListener(new MyOnPushLoadMoreListener(this));

            BuildDatas();

            _myRecyclerAdapter.NotifyDataSetChanged();
        }

        private View CreateFooterView()
        {
            var footerView = LayoutInflater.From(_swipeRefreshLayout.Context)
                    .Inflate(Resource.Layout.layout_footer, null);
            _footerProgressBar = (ProgressBar)footerView
                    .FindViewById(Resource.Id.footer_pb_view);
            _footerImageView = (ImageView)footerView
                    .FindViewById(Resource.Id.footer_image_view);
            _footerTextView = (TextView)footerView
                    .FindViewById(Resource.Id.footer_text_view);
            _footerProgressBar.Visibility = ViewStates.Gone;
            _footerImageView.Visibility = ViewStates.Visible;
            _footerImageView.SetImageResource(Resource.Drawable.down_arrow);
            _footerTextView.Text = "資料載入中....";
            return footerView;
        }

        private View CreateHeaderView()
        {
            var headerView = LayoutInflater.From(_swipeRefreshLayout.Context)
                    .Inflate(Resource.Layout.layout_head, null);
            _progressBar = (ProgressBar)headerView.FindViewById(Resource.Id.pb_view);
            _textView = (TextView)headerView.FindViewById(Resource.Id.text_view);
            _textView.Text = "資料更新中...";
            _imageView = (ImageView)headerView.FindViewById(Resource.Id.image_view);
            _imageView.Visibility = ViewStates.Visible;
            _imageView.SetImageResource(Resource.Drawable.down_arrow);
            _progressBar.Visibility = ViewStates.Gone;
            return headerView;
        }

        private void BuildDatas()
        {
            for (int i = 1; i <= 50; i++)
            {
                _dataList.Add("item " + (_dataList.Count + 1));
            }
        }

        private class MyOnPullRefreshListener : SuperSwipeRefreshLayout.IOnPullRefreshListener
        {
            private readonly RecyclerViewActivity _context;

            public MyOnPullRefreshListener(RecyclerViewActivity context)
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

                    //更新資料完成後，需更新畫面
                    _context._myRecyclerAdapter.NotifyDataSetChanged();
                };

                new Handler().PostDelayed(myAction, 2000);

                //模擬更新資料
                //Begin
                _context._dataList.Clear();
                _context.BuildDatas();
                //End
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

        private class MyOnPushLoadMoreListener : SuperSwipeRefreshLayout.IOnPushLoadMoreListener
        {
            private readonly RecyclerViewActivity _context;

            public MyOnPushLoadMoreListener(RecyclerViewActivity context)
            {
                _context = context;
            }

            void SuperSwipeRefreshLayout.IOnPushLoadMoreListener.OnLoadMore()
            {
                _context._footerTextView.Text = "讀取更多資料中...";
                _context._footerImageView.Visibility = ViewStates.Gone;
                _context._footerProgressBar.Visibility = ViewStates.Visible;

                Action myAction = () =>
                {

                    _context._footerImageView.Visibility = ViewStates.Visible;
                    _context._progressBar.Visibility = ViewStates.Gone;
                    _context._swipeRefreshLayout.SetLoadMore(false);

                    //資料讀取更多資料完成後，需更新畫面
                    _context._myRecyclerAdapter.NotifyDataSetChanged();
                };
                
                new Handler().PostDelayed(myAction, 5000);

                //模擬讀取更多資料
                //Begin
                _context.BuildDatas();
                //End
            }

            void SuperSwipeRefreshLayout.IOnPushLoadMoreListener.OnPushDistance(int distance)
            {

            }

            void SuperSwipeRefreshLayout.IOnPushLoadMoreListener.OnPushEnable(bool enable)
            {
                _context._footerTextView.Text = enable ? "放開 讀取更多資料" : "上拉 讀取更多資料";
                _context._footerImageView.Visibility = ViewStates.Visible;
                _context._footerImageView.Rotation = enable ? 0 : 180;
            }
        }
    }
}