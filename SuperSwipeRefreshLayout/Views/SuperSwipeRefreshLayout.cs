using System;
using Android.Animation;
using Android.Annotation;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Java.Lang;


namespace SuperSwipeRefreshLayoutDemoApp.Views
{

    public class SuperSwipeRefreshLayout : ViewGroup
    {
        private static readonly string LOG_TAG = "CustomeSwipeRefreshLayout";
        private static readonly int HEADER_VIEW_HEIGHT = 50;

        private static readonly float DECELERATE_INTERPOLATION_FACTOR = 2f;
        private static readonly int INVALID_POINTER = -1;
        private static readonly float DRAG_RATE = .5f;

        private static readonly int SCALE_DOWN_DURATION = 150;
        private static readonly int ANIMATE_TO_TRIGGER_DURATION = 200;
        private static readonly int ANIMATE_TO_START_DURATION = 200;
        private static readonly int DEFAULT_CIRCLE_TARGET = 64;


        private View _mTarget;

        private IOnPullRefreshListener _mOnPullRefreshListener;
        private IOnPushLoadMoreListener _mOnPushLoadMoreListener;

        private bool _mRefreshing = false;
        private bool _mLoadMore = false;
        private readonly int _mTouchSlop;
        private float _mTotalDragDistance = -1;
        private readonly int _mMediumAnimationDuration;
        private int _mCurrentTargetOffsetTop;
        private bool _mOriginalOffsetCalculated = false;

        private float _mInitialMotionY;
        private bool _mIsBeingDragged;
        private int _mActivePointerId = INVALID_POINTER;
        private readonly bool mScale = false;

        private bool _mReturningToStart;
        private readonly DecelerateInterpolator _mDecelerateInterpolator;
        private static readonly int[] LayoutAttrs = new int[] {Android.Resource.Attribute.Enabled};

        private HeadViewContainer _mHeadViewContainer;
        private RelativeLayout _mFooterViewContainer;
        private int _mHeaderViewIndex = -1;
        private int _mFooterViewIndex = -1;

        protected int MFrom;

        private float _mStartingScale;

        protected int MOriginalOffsetTop;

        private Animation _mScaleAnimation;

        private Animation _mScaleDownAnimation;

        private Animation _mScaleDownToStartAnimation;


        private readonly float _mSpinnerFinalOffset;

        private bool _mNotify;

        private readonly int _mHeaderViewWidth;

        private readonly int _mFooterViewWidth;

        private readonly int _mHeaderViewHeight;

        private readonly int _mFooterViewHeight;

        private readonly bool mUsingCustomStart = false;

        private bool _targetScrollWithLayout = true;

        private int _pushDistance = 0;

        private readonly CircleProgressView _defaultProgressView = null;

        private bool _usingDefaultHeader = true;

        private static float _density = 1.0f;

        private bool _isProgressEnable = true;

        private readonly Animation.IAnimationListener _mRefreshAnimationListener;
        //請參考ScaleAnimationListener的實作Animation.IAnimationListener

        //由於C#沒有像Java的匿名內部類別能直接實作介面或抽象類別，所以需要先將會用到的介面或抽象類別實作的變數宣告出來
        //Begin
        private readonly Animation _mAnimateToCorrectPosition;
        private readonly Animation _mAnimateToStartPosition;
        //Eed

        private class RefreshAnimationListener : Java.Lang.Object, Animation.IAnimationListener
        {
            private readonly SuperSwipeRefreshLayout _ssrl;

            public RefreshAnimationListener(SuperSwipeRefreshLayout ssrl)
            {
                _ssrl = ssrl;
            }

            void Animation.IAnimationListener.OnAnimationStart(Animation animation)
            {
                _ssrl._isProgressEnable = false;
            }

            void Animation.IAnimationListener.OnAnimationEnd(Animation animation)
            {
                _ssrl._isProgressEnable = true;
                if (_ssrl._mRefreshing)
                {
                    if (_ssrl._mNotify)
                    {
                        if (_ssrl._usingDefaultHeader)
                        {
                            ViewCompat.SetAlpha(_ssrl._defaultProgressView, 1.0f);
                            _ssrl._defaultProgressView.SetOnDraw(true);
                            new Thread(_ssrl._defaultProgressView).Start();
                        }
                        if (_ssrl._mOnPullRefreshListener != null)
                        {
                            _ssrl._mOnPullRefreshListener.OnRefresh();
                        }
                    }
                }
                else
                {
                    _ssrl._mHeadViewContainer.Visibility = ViewStates.Gone;
                    if (_ssrl.mScale)
                    {
                        _ssrl.SetAnimationProgress(0);
                    }
                    else
                    {
                        _ssrl.SetTargetOffsetTopAndBottom(_ssrl.MOriginalOffsetTop
                                                          - _ssrl._mCurrentTargetOffsetTop, true);
                    }
                }
                _ssrl._mCurrentTargetOffsetTop = _ssrl._mHeadViewContainer.Top;
                _ssrl.UpdateListenerCallBack();
            }

            void Animation.IAnimationListener.OnAnimationRepeat(Animation animation)
            {

            }
        }

        private void UpdateListenerCallBack()
        {
            int distance = _mCurrentTargetOffsetTop + _mHeadViewContainer.Height;
            if (_mOnPullRefreshListener != null)
            {
                _mOnPullRefreshListener.OnPullDistance(distance);
            }
            if (_usingDefaultHeader && _isProgressEnable)
            {
                _defaultProgressView.SetPullDistance(distance);
            }
        }

        public void SetHeaderView(View child)
        {
            if (child == null)
                return;

            if (_mHeadViewContainer == null)
                return;
            _usingDefaultHeader = false;
            _mHeadViewContainer.RemoveAllViews();
            var layoutParams = new RelativeLayout.LayoutParams(
                    _mHeaderViewWidth, _mHeaderViewHeight);
            layoutParams.AddRule(LayoutRules.AlignParentBottom);
            _mHeadViewContainer.AddView(child, layoutParams);
        }

        public void SetFooterView(View child)
        {
            if (child == null)
            {
                return;
            }
            if (_mFooterViewContainer == null)
            {
                return;
            }
            _mFooterViewContainer.RemoveAllViews();
            var layoutParams = new RelativeLayout.LayoutParams(
                    _mFooterViewWidth, _mFooterViewHeight);
            _mFooterViewContainer.AddView(child, layoutParams);
        }

        public SuperSwipeRefreshLayout(Context context) : this(context, null) { }

        public SuperSwipeRefreshLayout(Context context, IAttributeSet attrs) : base(context, attrs)
        {

            _mTouchSlop = ViewConfiguration.Get(context).ScaledTouchSlop;

            _mMediumAnimationDuration = Android.Resource.Integer.ConfigLongAnimTime;

            SetWillNotDraw(false);

            _mDecelerateInterpolator = new DecelerateInterpolator(
                DECELERATE_INTERPOLATION_FACTOR);

            var typeArray = context.ObtainStyledAttributes(attrs, LayoutAttrs);

            Enabled = typeArray.GetBoolean(0, true);
            typeArray.Recycle();

            //超雷的...
            //https://forums.xamarin.com/discussion/7272/get-windowmanager-using-getsystemservice-where-is-the-class-windowmanager
            var wm = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

            var display = wm.DefaultDisplay;

            var metrics = new DisplayMetrics();
            display.GetMetrics(metrics);

            var displaySize = new Point();
            display.GetSize(displaySize);

            _mHeaderViewWidth = displaySize.X;
            _mFooterViewWidth = displaySize.Y;
            _mHeaderViewHeight = (int)(HEADER_VIEW_HEIGHT * metrics.Density);
            _mFooterViewHeight = (int)(HEADER_VIEW_HEIGHT * metrics.Density);
            _defaultProgressView = new CircleProgressView(Context);
            CreateHeaderViewContainer();
            CreateFooterViewContainer();
            ViewCompat.SetChildrenDrawingOrderEnabled(this, true);
            _mSpinnerFinalOffset = DEFAULT_CIRCLE_TARGET * metrics.Density;
            _density = metrics.Density;
            _mTotalDragDistance = _mSpinnerFinalOffset;

            _mRefreshAnimationListener = new RefreshAnimationListener(this);

            _mAnimateToCorrectPosition = new ToCorrectPositionAnimation(this);

            _mAnimateToStartPosition = new ToStartPositionAnimate(this);

        }

        protected override int GetChildDrawingOrder(int childCount, int i)
        {
            
            if (_mHeaderViewIndex < 0 && _mFooterViewIndex < 0)
            {
                return i;
            }
            if (i == childCount - 2)
            {
                return _mHeaderViewIndex;
            }
            if (i == childCount - 1)
            {
                return _mFooterViewIndex;
            }
            int bigIndex = _mFooterViewIndex > _mHeaderViewIndex ? _mFooterViewIndex
                    : _mHeaderViewIndex;
            int smallIndex = _mFooterViewIndex < _mHeaderViewIndex ? _mFooterViewIndex
                    : _mHeaderViewIndex;
            if (i >= smallIndex && i < bigIndex - 1)
            {
                return i + 1;
            }
            if (i >= bigIndex || (i == bigIndex - 1))
            {
                return i + 2;
            }
            return i;
        }
        private void CreateHeaderViewContainer()
        {
           var layoutParams = new RelativeLayout.LayoutParams(
                (int)(_mHeaderViewHeight * 0.8),
                (int)(_mHeaderViewHeight * 0.8));
            layoutParams.AddRule(LayoutRules.CenterHorizontal);
            layoutParams.AddRule(LayoutRules.AlignParentBottom);
            _mHeadViewContainer = new HeadViewContainer(Context);
            _mHeadViewContainer.Visibility = ViewStates.Gone;
            _defaultProgressView.Visibility = ViewStates.Visible;
            _defaultProgressView.SetOnDraw(false);
            _mHeadViewContainer.AddView(_defaultProgressView, layoutParams);
            AddView(_mHeadViewContainer);
        }
        private void CreateFooterViewContainer()
        {
            _mFooterViewContainer = new RelativeLayout(Context);
            _mFooterViewContainer.Visibility = ViewStates.Gone;
            AddView(_mFooterViewContainer);
        }


        public void SetOnPullRefreshListener(IOnPullRefreshListener listener)
        {
            _mOnPullRefreshListener = listener;
        }

        public void SetHeaderViewBackgroundColor(int color)
        {
            _mHeadViewContainer.SetBackgroundColor(new Color(color));
        }
        public void SetOnPushLoadMoreListener(IOnPushLoadMoreListener onPushLoadMoreListener)
        {
            _mOnPushLoadMoreListener = onPushLoadMoreListener;
        }
        public void SetRefreshing(bool refreshing)
        {
            if (refreshing && _mRefreshing != refreshing)
            {
                // scale and show
                _mRefreshing = refreshing;
                int endTarget = 0;
                if (!mUsingCustomStart)
                {
                    endTarget = (int)(_mSpinnerFinalOffset + MOriginalOffsetTop);
                }
                else
                {
                    endTarget = (int)_mSpinnerFinalOffset;
                }
                SetTargetOffsetTopAndBottom(endTarget - _mCurrentTargetOffsetTop, true /* requires update */);
                _mNotify = false;
                StartScaleUpAnimation(_mRefreshAnimationListener);
            }
            else
            {
                SetRefreshing(refreshing, false /* notify */);
                if (_usingDefaultHeader)
                {
                    _defaultProgressView.SetOnDraw(false);
                }
            }
        }

        private void StartScaleUpAnimation(Animation.IAnimationListener listener)
        {
            _mHeadViewContainer.Visibility = ViewStates.Visible;

            _mScaleAnimation = new ScaleAnimation(this) {Duration = _mMediumAnimationDuration};
            //實作類別請參考ScaleAnimation

            //Before Refactor
            //_mScaleAnimation = new ScaleAnimation(this);
            //_mScaleAnimation.Duration = _mMediumAnimationDuration;

            if (listener != null)
            {
                _mHeadViewContainer.SetAnimationListener(listener);
            }
            _mHeadViewContainer.ClearAnimation();
            _mHeadViewContainer.StartAnimation(_mScaleAnimation);
        }
        private void SetAnimationProgress(float progress)
        {
            if (!_usingDefaultHeader)
            {
                progress = 1;
            }
            ViewCompat.SetScaleX(_mHeadViewContainer, progress);
            ViewCompat.SetScaleY(_mHeadViewContainer, progress);
        }
        private void SetRefreshing(bool refreshing, bool notify)
        {
            if (_mRefreshing == refreshing) return;
            _mNotify = notify;
            EnsureTarget();
            _mRefreshing = refreshing;
            if (_mRefreshing)
            {
                AnimateOffsetToCorrectPosition(_mCurrentTargetOffsetTop, _mRefreshAnimationListener);
            }
            else
            {
                StartScaleDownAnimation(_mRefreshAnimationListener);
            }

            //Before Refactor
            //if (_mRefreshing != refreshing)
            //{
            //    _mNotify = notify;
            //    EnsureTarget();
            //    _mRefreshing = refreshing;
            //    if (_mRefreshing)
            //    {
            //        AnimateOffsetToCorrectPosition(_mCurrentTargetOffsetTop, _mRefreshAnimationListener);
            //    }
            //    else
            //    {
            //        StartScaleDownAnimation(_mRefreshAnimationListener);
            //    }
            //}
        }
        private void StartScaleDownAnimation(Animation.IAnimationListener listener)
        {
            _mScaleDownAnimation = new ScaleDownAnimation(this)
            {
                Duration = SCALE_DOWN_DURATION
            };
            //實作類別請參考ScaleDownAnimation

            _mHeadViewContainer.SetAnimationListener(listener);
            _mHeadViewContainer.ClearAnimation();
            _mHeadViewContainer.StartAnimation(_mScaleDownAnimation);
        }

        public bool IsRefreshing()
        {
            return _mRefreshing;
        }
        private void EnsureTarget()
        {
            if (_mTarget != null) return;
            for (int i = 0; i < ChildCount; i++)
            {
                var child = this.GetChildAt(i);

                if (child.Equals(_mHeadViewContainer) || child.Equals(_mFooterViewContainer)) continue;
                _mTarget = child;
                break;

                //Before Refactor
                //if (!child.Equals(_mHeadViewContainer)
                //    && !child.Equals(_mFooterViewContainer))
                //{
                //    _mTarget = child;
                //    break;
                //}
            }
        }

        public void SetDistanceToTriggerSync(int distance)
        {
            _mTotalDragDistance = distance;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            int width = MeasuredWidth;
            int height = MeasuredHeight;
            if (ChildCount == 0)
            {
                return;
            }
            if (_mTarget == null)
            {
                EnsureTarget();
            }
            if (_mTarget == null)
            {
                return;
            }
            int distance = _mCurrentTargetOffsetTop + _mHeadViewContainer.Height;
            if (!_targetScrollWithLayout)
            {

                distance = 0;
            }
            var child = _mTarget;
            int childLeft = PaddingLeft;
            int childTop = PaddingTop + distance - _pushDistance;
            int childWidth = width - PaddingLeft - PaddingRight;
            int childHeight = height - PaddingTop - PaddingBottom;
            Log.Debug(LOG_TAG, "debug:onLayout childHeight = " + childHeight);
            child.Layout(childLeft, childTop, childLeft + childWidth, childTop + childHeight);
            int headViewWidth = _mHeadViewContainer.MeasuredWidth;
            int headViewHeight = _mHeadViewContainer.MeasuredHeight;
            _mHeadViewContainer.Layout((width/2 - headViewWidth/2),
                _mCurrentTargetOffsetTop, (width/2 + headViewWidth/2),
                _mCurrentTargetOffsetTop + headViewHeight);
            int footViewWidth = _mFooterViewContainer.MeasuredWidth;
            int footViewHeight = _mFooterViewContainer.MeasuredHeight;
            _mFooterViewContainer.Layout((width/2 - footViewWidth/2), height
                                                                     - _pushDistance, (width/2 + footViewWidth/2), height
                                                                                                                  +
                                                                                                                  footViewHeight -
                                                                                                                  _pushDistance);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            if (_mTarget == null)
            {
                EnsureTarget();
            }
            if (_mTarget == null)
            {
                return;
            }
            _mTarget.Measure(MeasureSpec.MakeMeasureSpec(MeasuredWidth
                                                        - PaddingLeft - PaddingRight, MeasureSpecMode.Exactly),
                MeasureSpec.MakeMeasureSpec(MeasuredHeight
                                            - PaddingTop - PaddingBottom,
                    MeasureSpecMode.Exactly));
            _mHeadViewContainer.Measure(MeasureSpec.MakeMeasureSpec(
                _mHeaderViewWidth, MeasureSpecMode.Exactly), MeasureSpec
                    .MakeMeasureSpec(3*_mHeaderViewHeight, MeasureSpecMode.Exactly));
            _mFooterViewContainer.Measure(MeasureSpec.MakeMeasureSpec(
                _mFooterViewWidth, MeasureSpecMode.Exactly), MeasureSpec
                    .MakeMeasureSpec(_mFooterViewHeight, MeasureSpecMode.Exactly));
            if (!mUsingCustomStart && !_mOriginalOffsetCalculated)
            {
                _mOriginalOffsetCalculated = true;
                _mCurrentTargetOffsetTop = MOriginalOffsetTop = -_mHeadViewContainer
                    .MeasuredHeight;
                UpdateListenerCallBack();
            }
            _mHeaderViewIndex = -1;
            for (int index = 0; index < ChildCount; index++)
            {
                if (GetChildAt(index) != _mHeadViewContainer) continue;
                _mHeaderViewIndex = index;
                break;

                //Before Refactor
                //if (GetChildAt(index) == mHeadViewContainer)
                //{
                //    mHeaderViewIndex = index;
                //    break;
                //}
            }
            _mFooterViewIndex = -1;
            for (int index = 0; index < ChildCount; index++)
            {
                if (GetChildAt(index) != _mFooterViewContainer) continue;
                _mFooterViewIndex = index;
                break;

                //Before Refactor
                //if (GetChildAt(index) == mFooterViewContainer)
                //{
                //    mFooterViewIndex = index;
                //    break;
                //}
            }
        }

        public bool IsChildScrollToTop()
        {
            if (Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.IceCreamSandwich)
            {
                if (_mTarget is AbsListView)
                {
                    var absListView = (AbsListView) _mTarget;
                    return !(absListView.ChildCount > 0 && (absListView
                        .FirstVisiblePosition > 0 || absListView
                            .GetChildAt(0).Top < absListView.PaddingTop));
                }
                else
                {
                    return !(_mTarget.ScrollY > 0);
                }
            }
            else
            {
                return !ViewCompat.CanScrollVertically(_mTarget, -1);
            }
        }


        public bool IsChildScrollToBottom()
        {
            if (IsChildScrollToTop())
            {
                return false;
            }

            if (_mTarget is RecyclerView)
            {
                var recyclerView = (RecyclerView) _mTarget;
                var layoutManager = recyclerView.GetLayoutManager();
                int count = recyclerView.GetAdapter().ItemCount;
                if (layoutManager is LinearLayoutManager && count > 0)
                {
                    var linearLayoutManager = (LinearLayoutManager) layoutManager;
                    if (linearLayoutManager.FindLastCompletelyVisibleItemPosition() == count - 1)
                    {
                        return true;
                    }
                }
                else if (layoutManager is StaggeredGridLayoutManager)
                {
                    var staggeredGridLayoutManager = (StaggeredGridLayoutManager) layoutManager;
                    int[] lastItems = new int[2];
                    staggeredGridLayoutManager
                        .FindLastCompletelyVisibleItemPositions(lastItems);
                    int lastItem = System.Math.Max(lastItems[0], lastItems[1]);
                    if (lastItem == count - 1)
                    {
                        return true;
                    }
                }
                return false;
            }

            if (_mTarget is AbsListView)
            //Before Refactor
            //else if (_mTarget is AbsListView)
            {
                var absListView = (AbsListView) _mTarget;
                int count = absListView.Adapter.Count;
                int fristPos = absListView.FirstVisiblePosition;
                if (fristPos == 0
                    && absListView.GetChildAt(0).Top >= absListView
                        .PaddingTop)
                {
                    return false;
                }
                int lastPos = absListView.LastVisiblePosition;

                return lastPos > 0 && count > 0 && lastPos == count - 1;

                //Before Refactor
                //if (lastPos > 0 && count > 0 && lastPos == count - 1)
                //{
                //    return true;
                //}
                //return false;
            }

            if (_mTarget is ScrollView)
            //Before Refactor
            //else if (_mTarget is ScrollView)
            {
                var scrollView = (ScrollView) _mTarget;
                var view =  scrollView.GetChildAt(scrollView.ChildCount - 1);

                if (view == null) return false;
                int diff = (view.Bottom - (scrollView.Height + scrollView.ScrollY));
                if (diff == 0)
                {
                    return true;
                }

                //Before Refactor
                //if (view != null)
                //{
                //    int diff = (view.Bottom - (scrollView.Height + scrollView
                //        .ScrollY));
                //    if (diff == 0)
                //    {
                //        return true;
                //    }
                //}
            }
            return false;
        }


        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            EnsureTarget();
            
            int action = MotionEventCompat.GetActionMasked(ev);
            //var action = ev.ActionMasked;
            //var action = ev.Action;

            if (_mReturningToStart && action == (int) MotionEventActions.Down)
            {
                _mReturningToStart = false;
            }
            if (!Enabled || _mReturningToStart || _mRefreshing || _mLoadMore
                || (!IsChildScrollToTop() && !IsChildScrollToBottom()))
            {

                return false;
            }

            switch (action)
            {
                case (int) MotionEventActions.Down:
                    SetTargetOffsetTopAndBottom(
                        MOriginalOffsetTop - _mHeadViewContainer.Top, true);
                    _mActivePointerId = MotionEventCompat.GetPointerId(ev, 0);
                    _mIsBeingDragged = false;
                    float initialMotionY = GetMotionEventY(ev, _mActivePointerId);
                    if (initialMotionY == -1)
                    {
                        return false;
                    }
                    _mInitialMotionY = initialMotionY;
                    break;
                case (int) MotionEventActions.Move:
                    if (_mActivePointerId == INVALID_POINTER)
                    {
                        Log.Error(LOG_TAG,
                            "Got ACTION_MOVE event but don't have an active pointer id.");
                        return false;
                    }

                    float y = GetMotionEventY(ev, _mActivePointerId);
                    if (y == -1)
                    {
                        return false;
                    }
                    float yDiff = 0;
                    if (IsChildScrollToBottom())
                    {
                        yDiff = _mInitialMotionY - y;
                        if (yDiff > _mTouchSlop && !_mIsBeingDragged)
                        {
                            _mIsBeingDragged = true;
                        }
                    }
                    else
                    {
                        yDiff = y - _mInitialMotionY;
                        if (yDiff > _mTouchSlop && !_mIsBeingDragged)
                        {
                            _mIsBeingDragged = true;
                        }
                    }
                    break;

                case MotionEventCompat.ActionPointerUp:
                    OnSecondaryPointerUp(ev);
                    break;

                case (int) MotionEventActions.Up:
                case (int) MotionEventActions.Cancel:
                    _mIsBeingDragged = false;
                    _mActivePointerId = INVALID_POINTER;
                    break;
            }

            return _mIsBeingDragged;
        }

        private float GetMotionEventY(MotionEvent ev, int activePointerId)
        {
            int index = MotionEventCompat.FindPointerIndex(ev,
                activePointerId);
            if (index < 0)
            {
                return -1;
            }
            return MotionEventCompat.GetY(ev, index);
        }

        public override void RequestDisallowInterceptTouchEvent(bool b)
        {
            // Nope.
        }


        public override bool OnTouchEvent(MotionEvent ev)
        {
            int action = MotionEventCompat.GetActionMasked(ev);

            if (_mReturningToStart && action == (int) MotionEventActions.Down)
            {
                _mReturningToStart = false;
            }
            if (!Enabled || _mReturningToStart || (!IsChildScrollToTop() && !IsChildScrollToBottom()))
            {
                return false;
            }

            return IsChildScrollToBottom() ? HandlerPushTouchEvent(ev, action) : HandlerPullTouchEvent(ev, action);

            //Before Refactor
            //if (IsChildScrollToBottom())
            //{
            //    return HandlerPushTouchEvent(ev, action);
            //}
            //else
            //{
            //    return HandlerPullTouchEvent(ev, action);
            //}
        }

        private bool HandlerPushTouchEvent(MotionEvent ev, int action)
        {
            switch (action)
            {
                case (int)MotionEventActions.Down:
                    _mActivePointerId = MotionEventCompat.GetPointerId(ev, 0);
                    _mIsBeingDragged = false;
                    Log.Debug(LOG_TAG, "debug:onTouchEvent ACTION_DOWN");
                    break;
                case (int)MotionEventActions.Move:
                    {
                        int pointerIndex = MotionEventCompat.FindPointerIndex(ev,
                                _mActivePointerId);
                        if (pointerIndex < 0)
                        {
                            Log.Error(LOG_TAG,
                                    "Got ACTION_MOVE event but have an invalid active pointer id.");
                            return false;
                        }
                         float y = MotionEventCompat.GetY(ev, pointerIndex);
                         float overscrollBottom = (_mInitialMotionY - y) * DRAG_RATE;
                        if (_mIsBeingDragged)
                        {
                            _pushDistance = (int)overscrollBottom;
                            UpdateFooterViewPosition();

                            _mOnPushLoadMoreListener?.OnPushEnable(_pushDistance >= _mFooterViewHeight);

                            //Before Refactor
                            //if (_mOnPushLoadMoreListener != null)
                            //{
                            //    _mOnPushLoadMoreListener
                            //            .OnPushEnable(_pushDistance >= _mFooterViewHeight);
                            //}
                        }
                        break;
                    }
                case MotionEventCompat.ActionPointerDown:
                    {
                        int index = MotionEventCompat.GetActionIndex(ev);
                        _mActivePointerId = MotionEventCompat.GetPointerId(ev, index);
                        break;
                    }

                case MotionEventCompat.ActionPointerUp:
                    OnSecondaryPointerUp(ev);
                    break;

                case (int)MotionEventActions.Up:
                case (int)MotionEventActions.Cancel:
                    {
                        if (_mActivePointerId == INVALID_POINTER)
                        {
                            if (action == (int)MotionEventActions.Up)
                            {
                                Log.Error(LOG_TAG,
                                        "Got ACTION_UP event but don't have an active pointer id.");
                            }
                            return false;
                        }
                        int pointerIndex = MotionEventCompat.FindPointerIndex(ev,
                          _mActivePointerId);
                        float y = MotionEventCompat.GetY(ev, pointerIndex);
                        float overscrollBottom = (_mInitialMotionY - y) * DRAG_RATE;
                        _mIsBeingDragged = false;
                        _mActivePointerId = INVALID_POINTER;
                        if (overscrollBottom < _mFooterViewHeight
                                || _mOnPushLoadMoreListener == null)
                        {
                            _pushDistance = 0;
                        }
                        else
                        {
                            _pushDistance = _mFooterViewHeight;
                        }
                        if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Honeycomb)
                        {
                            UpdateFooterViewPosition();

                            if (_pushDistance != _mFooterViewHeight || _mOnPushLoadMoreListener == null) return false;
                            _mLoadMore = true;
                            _mOnPushLoadMoreListener.OnLoadMore();

                            //Before Refactor
                            //if (_pushDistance == _mFooterViewHeight
                            //        && _mOnPushLoadMoreListener != null)
                            //{
                            //    _mLoadMore = true;
                            //    _mOnPushLoadMoreListener.OnLoadMore();
                            //}
                        }
                        else
                        {
                            AnimatorFooterToBottom((int)overscrollBottom, _pushDistance);
                        }
                        return false;
                    }
            }
            return true;
        }

        private bool HandlerPullTouchEvent(MotionEvent ev, int action)
        {
            switch (action)
            {
                case (int) MotionEventActions.Down:
                    _mActivePointerId = MotionEventCompat.GetPointerId(ev, 0);
                    _mIsBeingDragged = false;
                    break;

                case (int) MotionEventActions.Move:
                {
                    int pointerIndex = MotionEventCompat.FindPointerIndex(ev, _mActivePointerId);
                    if (pointerIndex < 0)
                    {
                        Log.Error(LOG_TAG,
                            "Got ACTION_MOVE event but have an invalid active pointer id.");
                        return false;
                    }

                    float y = MotionEventCompat.GetY(ev, pointerIndex);
                    float overscrollTop = (y - _mInitialMotionY)*DRAG_RATE;
                    if (_mIsBeingDragged)
                    {
                        float originalDragPercent = overscrollTop/_mTotalDragDistance;
                        if (originalDragPercent < 0)
                        {
                            return false;
                        }
                        float dragPercent = System.Math.Min(1f, System.Math.Abs(originalDragPercent));
                        float extraOs = System.Math.Abs(overscrollTop) - _mTotalDragDistance;
                        float slingshotDist = mUsingCustomStart ? _mSpinnerFinalOffset - MOriginalOffsetTop : _mSpinnerFinalOffset;
                        float tensionSlingshotPercent = System.Math.Max(0,
                            System.Math.Min(extraOs, slingshotDist*2)/slingshotDist);
                        float tensionPercent = (float) ((tensionSlingshotPercent/4) - System.Math
                            .Pow((tensionSlingshotPercent/4), 2))*2f;
                        float extraMove = (slingshotDist)*tensionPercent*2;

                        int targetY = MOriginalOffsetTop
                                      + (int) ((slingshotDist*dragPercent) + extraMove);
                        if (_mHeadViewContainer.Visibility != ViewStates.Visible)
                        {
                            _mHeadViewContainer.Visibility = ViewStates.Visible;
                        }
                        if (!mScale)
                        {
                            ViewCompat.SetScaleX(_mHeadViewContainer, 1f);
                            ViewCompat.SetScaleY(_mHeadViewContainer, 1f);
                        }
                        if (_usingDefaultHeader)
                        {
                            float alpha = overscrollTop/_mTotalDragDistance;
                            if (alpha >= 1.0f)
                            {
                                alpha = 1.0f;
                            }
                            ViewCompat.SetScaleX(_defaultProgressView, alpha);
                            ViewCompat.SetScaleY(_defaultProgressView, alpha);
                            ViewCompat.SetAlpha(_defaultProgressView, alpha);
                        }
                        if (overscrollTop < _mTotalDragDistance)
                        {
                            if (mScale)
                            {
                                SetAnimationProgress(overscrollTop/_mTotalDragDistance);
                            }

                            _mOnPullRefreshListener?.OnPullEnable(false);
                            
                            //Before Refactor
                            //if (_mOnPullRefreshListener != null)
                            //{
                            //    _mOnPullRefreshListener.OnPullEnable(false);
                            //}
                        }
                        else
                        {
                            _mOnPullRefreshListener?.OnPullEnable(true);

                            //Before Refactor
                            //if (_mOnPullRefreshListener != null)
                            //{
                            //    _mOnPullRefreshListener.OnPullEnable(true);
                            //}
                        }
                        SetTargetOffsetTopAndBottom(targetY - _mCurrentTargetOffsetTop, true);
                    }
                    break;
                }
                case MotionEventCompat.ActionPointerDown:
                {
                    int index = MotionEventCompat.GetActionIndex(ev);
                    _mActivePointerId = MotionEventCompat.GetPointerId(ev, index);
                    break;
                }

                case MotionEventCompat.ActionPointerUp:
                    OnSecondaryPointerUp(ev);
                    break;

                case (int) MotionEventActions.Up:
                case (int) MotionEventActions.Cancel:
                {
                    if (_mActivePointerId == INVALID_POINTER)
                    {
                        if (action == (int) MotionEventActions.Up)
                        {
                            Log.Error(LOG_TAG,"Got ACTION_UP event but don't have an active pointer id.");
                        }
                        return false;
                    }
                    int pointerIndex = MotionEventCompat.FindPointerIndex(ev, _mActivePointerId);
                    float y = MotionEventCompat.GetY(ev, pointerIndex);
                    float overscrollTop = (y - _mInitialMotionY)*DRAG_RATE;
                    _mIsBeingDragged = false;
                    if (overscrollTop > _mTotalDragDistance)
                    {
                        SetRefreshing(true, true /* notify */);
                    }
                    else
                    {
                        _mRefreshing = false;
                        Animation.IAnimationListener listener = null;
                        if (!mScale)
                        {
                            listener = new ScaleAnimationListener(this);
                            //請參考ScaleAnimationListener實作Animation.IAnimationListener
                        }

                        AnimateOffsetToStartPosition(_mCurrentTargetOffsetTop, listener);
                    }
                }
                _mActivePointerId = INVALID_POINTER;
                return false;
            }
            return true;
        }
        private class ScaleAnimationListener : Java.Lang.Object, Animation.IAnimationListener
        {
            private readonly SuperSwipeRefreshLayout _ssrl;

            public ScaleAnimationListener(SuperSwipeRefreshLayout ssrl)
            {
                _ssrl = ssrl;
            }

            public void OnAnimationEnd(Animation animation)
            {
                if (!_ssrl.mScale)
                {
                    _ssrl.StartScaleDownAnimation(null);
                }
            }

            public void OnAnimationRepeat(Animation animation)
            {

            }

            public void OnAnimationStart(Animation animation)
            {

            }
        }

        [TargetApi(Value = (int)BuildVersionCodes.Honeycomb)]
        private void AnimatorFooterToBottom(int start, int end)
        {
            var valueAnimator = ValueAnimator.OfInt(start, end);
            valueAnimator.SetDuration(50);

            valueAnimator.Update += (sender, args) =>
            {

                // update
                _pushDistance = (int)valueAnimator.AnimatedValue;
                UpdateFooterViewPosition();
            };
            valueAnimator.AnimationEnd += (sender, args) =>
            {
                if (end > 0 && _mOnPushLoadMoreListener != null)
                {
                    // start loading more
                    _mLoadMore = true;
                    _mOnPushLoadMoreListener.OnLoadMore();
                }
                else
                {
                    ResetTargetLayout();
                    _mLoadMore = false;
                }

            };

            valueAnimator.SetInterpolator(_mDecelerateInterpolator);
            valueAnimator.Start();
        }

        private void AnimateOffsetToStartPosition(int from, Animation.IAnimationListener listener)
        {
            if (mScale)
            {
                StartScaleDownReturnToStartAnimation(from, listener);
            }
            else
            {
                MFrom = from;
                _mAnimateToStartPosition.Reset();
                _mAnimateToStartPosition.Duration = ANIMATE_TO_START_DURATION;
                _mAnimateToStartPosition.Interpolator = _mDecelerateInterpolator;
                if (listener != null)
                {
                    _mHeadViewContainer.SetAnimationListener(listener);
                }
                _mHeadViewContainer.ClearAnimation();
                _mHeadViewContainer.StartAnimation(_mAnimateToStartPosition);
            }
            ResetTargetLayoutDelay(ANIMATE_TO_START_DURATION);
        }

        public void SetLoadMore(bool loadMore)
        {
            if (loadMore || !_mLoadMore) return;
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Honeycomb)
            {
                _mLoadMore = false;
                _pushDistance = 0;
                UpdateFooterViewPosition();
            }
            else
            {
                AnimatorFooterToBottom(_mFooterViewHeight, 0);
            }

            //Before Refactor
            //if (!loadMore && _mLoadMore)
            //{
            //    if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Honeycomb)
            //    {
            //        _mLoadMore = false;
            //        _pushDistance = 0;
            //        UpdateFooterViewPosition();
            //    }
            //    else
            //    {
            //        AnimatorFooterToBottom(_mFooterViewHeight, 0);
            //    }
            //}
        }

        private void AnimateOffsetToCorrectPosition(int from, Animation.IAnimationListener listener)
        {
            MFrom = from;
            _mAnimateToCorrectPosition.Reset();
            _mAnimateToCorrectPosition.Duration = ANIMATE_TO_TRIGGER_DURATION;
            _mAnimateToCorrectPosition.Interpolator = _mDecelerateInterpolator;
            if (listener != null)
            {
                _mHeadViewContainer.SetAnimationListener(listener);
            }
            _mHeadViewContainer.ClearAnimation();
            _mHeadViewContainer.StartAnimation(_mAnimateToCorrectPosition);
        }

        public void ResetTargetLayoutDelay(int delay)
        {
            Action myAction = ResetTargetLayout;

            new Handler().PostDelayed(myAction, delay);
        }

        public void ResetTargetLayout()
        {
            int width = MeasuredWidth;
            int height = MeasuredHeight;
            View child = _mTarget;
            int childLeft = PaddingLeft;
            int childTop = PaddingTop;
            int childWidth = child.Width - PaddingLeft
                             - PaddingRight;
            int childHeight = child.Height - PaddingTop
                              - PaddingBottom;
            child.Layout(childLeft, childTop, childLeft + childWidth, childTop + childHeight);

            int headViewWidth = _mHeadViewContainer.MeasuredWidth;
            int headViewHeight = _mHeadViewContainer.MeasuredHeight;
            _mHeadViewContainer.Layout((width/2 - headViewWidth/2),-headViewHeight, (width/2 + headViewWidth/2), 0);
            int footViewWidth = _mFooterViewContainer.MeasuredWidth;
            int footViewHeight = _mFooterViewContainer.MeasuredHeight;
            _mFooterViewContainer.Layout((width/2 - footViewWidth/2), height,(width/2 + footViewWidth/2), height + footViewHeight);
        }

        //mAnimationToCorrectPosition的物件設定在建構函式中
        private class ToCorrectPositionAnimation : Animation

        {
            private readonly SuperSwipeRefreshLayout _ssrl;

            public ToCorrectPositionAnimation(SuperSwipeRefreshLayout ssrl) : base()
            {
                _ssrl = ssrl;
            }

            protected override void ApplyTransformation(float interpolatedTime, Transformation t)
            {
                int targetTop = 0;
                int endTarget = 0;
                if (!_ssrl.mUsingCustomStart)
                {
                    endTarget = (int)(_ssrl._mSpinnerFinalOffset - System.Math
                        .Abs(_ssrl.MOriginalOffsetTop));
                }
                else
                {
                    endTarget = (int)_ssrl._mSpinnerFinalOffset;
                }
                targetTop = (_ssrl.MFrom + (int)((endTarget - _ssrl.MFrom) * interpolatedTime));
                int offset = targetTop - _ssrl._mHeadViewContainer.Top;
                _ssrl.SetTargetOffsetTopAndBottom(offset, false /* requires update */);
            }

        }

        private void MoveToStart(float interpolatedTime)
        {
            int targetTop = 0;
            targetTop = (MFrom + (int)((MOriginalOffsetTop - MFrom) * interpolatedTime));
            int offset = targetTop - _mHeadViewContainer.Top;
            SetTargetOffsetTopAndBottom(offset, false /* requires update */);
        }

        //mAnimationToStartPosition的物件設定在建構函式中
        private class ToStartPositionAnimate : Animation
        {
            private readonly SuperSwipeRefreshLayout _ssrl;

            public ToStartPositionAnimate(SuperSwipeRefreshLayout ssrl) : base()
            {
                _ssrl = ssrl;
            }

            protected override void ApplyTransformation(float interpolatedTime,
                Transformation t)
            {
                _ssrl.MoveToStart(interpolatedTime);
            }
        }

        private void StartScaleDownReturnToStartAnimation(int from, Animation.IAnimationListener listener)
        {
            MFrom = from;
            _mStartingScale = ViewCompat.GetScaleX(_mHeadViewContainer);
            _mScaleDownToStartAnimation = new ScaleDownToStartAnimation(this);
            //實作類別請參考ScaleDownToStartAnimation

            _mScaleDownToStartAnimation.Duration = SCALE_DOWN_DURATION;
            if (listener != null)
            {
                _mHeadViewContainer.SetAnimationListener(listener);
            }
            _mHeadViewContainer.ClearAnimation();
            _mHeadViewContainer.StartAnimation(_mScaleDownToStartAnimation);
        }

        private void SetTargetOffsetTopAndBottom(int offset, bool requiresUpdate)
        {
            _mHeadViewContainer.BringToFront();
            _mHeadViewContainer.OffsetTopAndBottom(offset);
            _mCurrentTargetOffsetTop = _mHeadViewContainer.Top;
            if (requiresUpdate && Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Honeycomb)
            {
                Invalidate();
            }
            UpdateListenerCallBack();
        }

        private void UpdateFooterViewPosition()
        {
            _mFooterViewContainer.Visibility = ViewStates.Visible;
            _mFooterViewContainer.BringToFront();
            _mFooterViewContainer.OffsetTopAndBottom(-_pushDistance);
            UpdatePushDistanceListener();
        }

        private void UpdatePushDistanceListener()
        {
            _mOnPushLoadMoreListener?.OnPushDistance(_pushDistance);

            //Before Refactor
            //if (_mOnPushLoadMoreListener != null)
            //{
            //    _mOnPushLoadMoreListener.OnPushDistance(_pushDistance);
            //}
        }

        private void OnSecondaryPointerUp(MotionEvent ev)
        {
            int pointerIndex = MotionEventCompat.GetActionIndex(ev);
            int pointerId = MotionEventCompat.GetPointerId(ev, pointerIndex);


            if (pointerId != _mActivePointerId) return;
            int newPointerIndex = pointerIndex == 0 ? 1 : 0;
            _mActivePointerId = MotionEventCompat.GetPointerId(ev, newPointerIndex);

            //Before Refactor
            //if (pointerId == _mActivePointerId)
            //{
            //    int newPointerIndex = pointerIndex == 0 ? 1 : 0;
            //    _mActivePointerId = MotionEventCompat.GetPointerId(ev,
            //        newPointerIndex);
            //}
        }

        private class HeadViewContainer : RelativeLayout
        {

            private Animation.IAnimationListener _mAnimationListener;

            public HeadViewContainer(Context context) : base(context)
            {

            }

            public void SetAnimationListener(Animation.IAnimationListener mAnimationListener)
            {
                _mAnimationListener = mAnimationListener;
            }

            protected override void OnAnimationStart()
            {
                base.OnAnimationStart();

                //取代下面ifstatment的作法
                _mAnimationListener?.OnAnimationStart(Animation);

                //Before Refactor
                //if (_mAnimationListener != null)
                //{
                //    _mAnimationListener.OnAnimationStart(Animation);
                //}
            }


            protected override void OnAnimationEnd()
            {
                base.OnAnimationEnd();

                //取代下面ifstatment的作法
                _mAnimationListener?.OnAnimationEnd(Animation);

                //Before Refactor
                //if (_mAnimationListener != null)
                //{
                //    _mAnimationListener.OnAnimationEnd(Animation);
                //}
            }
        }
        
        public bool IsTargetScrollWithLayout()
        {
            return _targetScrollWithLayout;
        }

        public void SetTargetScrollWithLayout(bool targetScrollWithLayout)
        {
            this._targetScrollWithLayout = targetScrollWithLayout;
        }

        public interface IOnPullRefreshListener
        {
            void OnRefresh();

            void OnPullDistance(int distance);

            void OnPullEnable(bool enable);
        }

        public interface IOnPushLoadMoreListener
        {
            void OnLoadMore();

            void OnPushDistance(int distance);

            void OnPushEnable(bool enable);
        }


        public void SetDefaultCircleProgressColor(uint color)
        {
            if (_usingDefaultHeader)
            {
                _defaultProgressView.SetProgressColor(color);
            }
        }


        public void SetDefaultCircleBackgroundColor(uint color)
        {
            if (_usingDefaultHeader)
            {
                _defaultProgressView.SetCircleBackgroundColor(color);
            }
        }

        public void SetDefaultCircleShadowColor(uint color)
        {
            if (_usingDefaultHeader)
            {
                _defaultProgressView.SetShadowColor(color);
            }
        }

        public class CircleProgressView : View, IRunnable
        {
            public bool IsRunning { get; private set; } = false;

            private static readonly int PEROID = 16;
            private Paint _progressPaint;
            private Paint _bgPaint;
            private int _width;
            private int _height;

            private bool _isOnDraw = false;

            private int startAngle = 0;
            private int _speed = 8;
            private RectF ovalRect = null;
            private RectF bgRect = null;
            private int swipeAngle;
            private uint _progressColor = 0xffcccccc;
            private uint _circleBackgroundColor = 0xffffffff;
            private uint _shadowColor = 0xff999999;

            public CircleProgressView(Context context) : base(context)
            {

            }

            public CircleProgressView(Context context, IAttributeSet attrs) : base(context, attrs)
            {

            }

            public CircleProgressView(Context context, IAttributeSet attrs,
                int defStyleAttr) : base(context, attrs, defStyleAttr)
            {

            }

            protected override void OnDraw(Canvas canvas)
            {
                base.OnDraw(canvas);
                canvas.DrawArc(GetBgRect(), 0, 360, false, CreateBgPaint());
                int index = startAngle/360;
                if (index%2 == 0)
                {
                    swipeAngle = (startAngle%720)/2;
                }
                else
                {
                    swipeAngle = 360 - (startAngle%720)/2;
                }
                canvas.DrawArc(GetOvalRect(), startAngle, swipeAngle, false, CreatePaint());
            }

            private RectF GetBgRect()
            {
                _width = Width;
                _height = Height;

                if (bgRect != null) return bgRect;
                int offset = (int) (_density*2);
                bgRect = new RectF(offset, offset, _width - offset, _height - offset);

                //Before Refactor
                //if (bgRect == null)
                //{
                //    int offset = (int)(_density * 2);
                //    bgRect = new RectF(offset, offset, _width - offset, _height
                //                                                        - offset);
                //}
                return bgRect;
            }

            private RectF GetOvalRect()
            {
                _width = Width;
                _height = Height;


                if (ovalRect != null) return ovalRect;
                int offset = (int) (_density*8);
                ovalRect = new RectF(offset, offset, _width - offset, _height - offset);

                //Before Refactor
                //if (ovalRect == null)
                //{
                //    int offset = (int)(_density * 8);
                //    ovalRect = new RectF(offset, offset, _width - offset, _height
                //                                                          - offset);
                //}
                return ovalRect;
            }

            public void SetProgressColor(uint progressColor)
            {
                _progressColor = progressColor;
            }

            public void SetCircleBackgroundColor(uint circleBackgroundColor)
            {
                _circleBackgroundColor = circleBackgroundColor;
            }

            public void SetShadowColor(uint shadowColor)
            {
                _shadowColor = shadowColor;
            }


            private Paint CreatePaint()
            {
                if (_progressPaint == null)
                {
                    _progressPaint = new Paint();
                    _progressPaint.StrokeWidth = (int) (_density*3);
                    _progressPaint.SetStyle(Paint.Style.Stroke);
                    _progressPaint.AntiAlias = true;
                }
                _progressPaint.Color = new Color((int) _progressColor);
                return _progressPaint;
            }

            private Paint CreateBgPaint()
            {
                if (_bgPaint != null) return _bgPaint;
                _bgPaint = new Paint()
                {
                    Color = new Color((int)_circleBackgroundColor),
                    AntiAlias = true
                };
                    
                _bgPaint.SetStyle(Paint.Style.Fill);
                  
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
                {
                    this.SetLayerType(LayerType.Software, _bgPaint);
                }
                _bgPaint.SetShadowLayer(4.0f, 0.0f, 2.0f, new Color((int) _shadowColor));

                //Before Refactor
                //if (_bgPaint == null)
                //{
                //    _bgPaint = new Paint()
                //    {
                //        Color = new Color((int)_circleBackgroundColor),
                //        AntiAlias = true
                //    };

                //    _bgPaint.SetStyle(Paint.Style.Fill);

                //    if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
                //    {
                //        this.SetLayerType(LayerType.Software, _bgPaint);
                //    }
                //    _bgPaint.SetShadowLayer(4.0f, 0.0f, 2.0f, new Color((int)_shadowColor));
                //}
                return _bgPaint;
            }

            public void SetPullDistance(int distance)
            {
                this.startAngle = distance*2;
                PostInvalidate();
            }


            public void Run()
            {
                while (_isOnDraw)
                {
                    IsRunning = true;
                    long startTime = System.Environment.TickCount;
                    startAngle += _speed;
                    PostInvalidate();
                    long time = System.Environment.TickCount - startTime;

                    if (time >= PEROID) continue;
                    try
                    {
                        Thread.Sleep(PEROID - time);
                    }
                    catch (InterruptedException e)
                    {
                        e.PrintStackTrace();
                    }

                    //Before Refactor
                    //if (time < PEROID)
                    //{
                    //    try
                    //    {
                    //        Thread.Sleep(PEROID - time);
                    //    }
                    //    catch (InterruptedException e)
                    //    {
                    //        e.PrintStackTrace();
                    //    }
                    //}
                }
            }

            public void SetOnDraw(bool isOnDraw)
            {
                _isOnDraw = isOnDraw;
            }

            public void SetSpeed(int speed)
            {
                _speed = speed;
            }

            public override void OnWindowFocusChanged(bool hasWindowFocus)
            {
                base.OnWindowFocusChanged(hasWindowFocus);
            }


            protected override void OnDetachedFromWindow()
            {
                _isOnDraw = false;
                base.OnDetachedFromWindow();
            }
        }

        //此為針對mScaleAnimation的Animation實作類別
        private class ScaleAnimation : Animation
        {
            private readonly SuperSwipeRefreshLayout _ssrl;

            public ScaleAnimation(SuperSwipeRefreshLayout ssrl) : base()
            {
                _ssrl = ssrl;
            }

            protected override void ApplyTransformation(float interpolatedTime, Transformation t)
            {
                _ssrl.SetAnimationProgress(interpolatedTime);
            }

            public override void SetAnimationListener(IAnimationListener listener)
            {
                base.SetAnimationListener(listener);
            }
        }

        //此為針對mScaleDownAnimation的Animation實作類別
        private class ScaleDownAnimation : Animation
        {
            private readonly SuperSwipeRefreshLayout _ssrl;

            public ScaleDownAnimation(SuperSwipeRefreshLayout ssrl) : base()
            {
                _ssrl = ssrl;
            }

            protected override void ApplyTransformation(float interpolatedTime,
                Transformation t)
            {
                _ssrl.SetAnimationProgress(1 - interpolatedTime);
            }

        }

        //此為針對mScaleDownToStartAnimation的Animation實作類別
        private class ScaleDownToStartAnimation : Animation
        {
            private readonly SuperSwipeRefreshLayout _ssrl;

            public ScaleDownToStartAnimation(SuperSwipeRefreshLayout ssrl) : base()
            {
                _ssrl = ssrl;
            }

            protected override void ApplyTransformation(float interpolatedTime, Transformation t)
            {
                float targetScale = (_ssrl._mStartingScale + (-_ssrl._mStartingScale * interpolatedTime));
                _ssrl.SetAnimationProgress(targetScale);
                _ssrl.MoveToStart(interpolatedTime);
            }

        }
    }

}