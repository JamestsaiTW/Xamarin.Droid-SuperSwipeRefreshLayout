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


        private View mTarget;

        private IOnPullRefreshListener mListener;
        private IOnPushLoadMoreListener mOnPushLoadMoreListener;

        private bool mRefreshing = false;
        private bool mLoadMore = false;
        private int mTouchSlop;
        private float mTotalDragDistance = -1;
        private int mMediumAnimationDuration;
        private int mCurrentTargetOffsetTop;
        private bool mOriginalOffsetCalculated = false;

        private float mInitialMotionY;
        private bool mIsBeingDragged;
        private int mActivePointerId = INVALID_POINTER;
        private readonly bool mScale = false;

        private bool mReturningToStart;
        private readonly DecelerateInterpolator mDecelerateInterpolator;
        private static readonly int[] LAYOUT_ATTRS = new int[] {Android.Resource.Attribute.Enabled};

        private HeadViewContainer mHeadViewContainer;
        private RelativeLayout mFooterViewContainer;
        private int mHeaderViewIndex = -1;
        private int mFooterViewIndex = -1;

        protected int mFrom;

        private float mStartingScale;

        protected int mOriginalOffsetTop;

        private Animation mScaleAnimation;

        private Animation mScaleDownAnimation;

        private Animation mScaleDownToStartAnimation;


        private readonly float mSpinnerFinalOffset;

        private bool mNotify;

        private readonly int mHeaderViewWidth;

        private readonly int mFooterViewWidth;

        private readonly int mHeaderViewHeight;

        private readonly int mFooterViewHeight;

        private readonly bool mUsingCustomStart = false;

        private bool targetScrollWithLayout = true;

        private int pushDistance = 0;

        private readonly CircleProgressView defaultProgressView = null;

        private bool usingDefaultHeader = true;

        private static float density = 1.0f;

        private bool isProgressEnable = true;

        private readonly Animation.IAnimationListener mRefreshAnimationListener;
        //請參考ScaleAnimationListener的實作Animation.IAnimationListener

        //由於C#沒有像Java的匿名內部類別能直接實作介面或抽象類別，所以需要先將會用到的介面或抽象類別實作的變數宣告出來
        //Begin
        private readonly Animation mAnimateToCorrectPosition;
        private readonly Animation mAnimateToStartPosition;
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
                _ssrl.isProgressEnable = false;
            }

            void Animation.IAnimationListener.OnAnimationEnd(Animation animation)
            {
                _ssrl.isProgressEnable = true;
                if (_ssrl.mRefreshing)
                {
                    if (_ssrl.mNotify)
                    {
                        if (_ssrl.usingDefaultHeader)
                        {
                            ViewCompat.SetAlpha(_ssrl.defaultProgressView, 1.0f);
                            _ssrl.defaultProgressView.SetOnDraw(true);
                            new Thread(_ssrl.defaultProgressView).Start();
                        }
                        if (_ssrl.mListener != null)
                        {
                            _ssrl.mListener.OnRefresh();
                        }
                    }
                }
                else
                {
                    _ssrl.mHeadViewContainer.Visibility = ViewStates.Gone;
                    if (_ssrl.mScale)
                    {
                        _ssrl.SetAnimationProgress(0);
                    }
                    else
                    {
                        _ssrl.SetTargetOffsetTopAndBottom(_ssrl.mOriginalOffsetTop
                                                          - _ssrl.mCurrentTargetOffsetTop, true);
                    }
                }
                _ssrl.mCurrentTargetOffsetTop = _ssrl.mHeadViewContainer.Top;
                _ssrl.UpdateListenerCallBack();
            }

            void Animation.IAnimationListener.OnAnimationRepeat(Animation animation)
            {

            }
        }

        private void UpdateListenerCallBack()
        {
            int distance = mCurrentTargetOffsetTop + mHeadViewContainer.Height;
            if (mListener != null)
            {
                mListener.OnPullDistance(distance);
            }
            if (usingDefaultHeader && isProgressEnable)
            {
                defaultProgressView.SetPullDistance(distance);
            }
        }

        public void SetHeaderView(View child)
        {
            if (child == null)
                return;

            if (mHeadViewContainer == null)
                return;
            usingDefaultHeader = false;
            mHeadViewContainer.RemoveAllViews();
            var layoutParams = new RelativeLayout.LayoutParams(
                    mHeaderViewWidth, mHeaderViewHeight);
            layoutParams.AddRule(LayoutRules.AlignParentBottom);
            mHeadViewContainer.AddView(child, layoutParams);
        }

        public void SetFooterView(View child)
        {
            if (child == null)
            {
                return;
            }
            if (mFooterViewContainer == null)
            {
                return;
            }
            mFooterViewContainer.RemoveAllViews();
            var layoutParams = new RelativeLayout.LayoutParams(
                    mFooterViewWidth, mFooterViewHeight);
            mFooterViewContainer.AddView(child, layoutParams);
        }

        public SuperSwipeRefreshLayout(Context context) : this(context, null) { }

        public SuperSwipeRefreshLayout(Context context, IAttributeSet attrs) : base(context, attrs)
        {

            mTouchSlop = ViewConfiguration.Get(context).ScaledTouchSlop;

            mMediumAnimationDuration = Android.Resource.Integer.ConfigLongAnimTime;

            SetWillNotDraw(false);

            mDecelerateInterpolator = new DecelerateInterpolator(
                DECELERATE_INTERPOLATION_FACTOR);

            TypedArray a = context.ObtainStyledAttributes(attrs, LAYOUT_ATTRS);

            this.Enabled = a.GetBoolean(0, true);
            a.Recycle();

            //超雷的...
            //https://forums.xamarin.com/discussion/7272/get-windowmanager-using-getsystemservice-where-is-the-class-windowmanager
            IWindowManager wm = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

            Display display = wm.DefaultDisplay;

            DisplayMetrics metrics = new DisplayMetrics();
            display.GetMetrics(metrics);

            Point displaySize = new Point();
            display.GetSize(displaySize);

            mHeaderViewWidth = displaySize.X;
            mFooterViewWidth = displaySize.Y;
            mHeaderViewHeight = (int)(HEADER_VIEW_HEIGHT * metrics.Density);
            mFooterViewHeight = (int)(HEADER_VIEW_HEIGHT * metrics.Density);
            defaultProgressView = new CircleProgressView(Context);
            CreateHeaderViewContainer();
            CreateFooterViewContainer();
            ViewCompat.SetChildrenDrawingOrderEnabled(this, true);
            mSpinnerFinalOffset = DEFAULT_CIRCLE_TARGET * metrics.Density;
            density = metrics.Density;
            mTotalDragDistance = mSpinnerFinalOffset;

            mRefreshAnimationListener = new RefreshAnimationListener(this);

            mAnimateToCorrectPosition = new ToCorrectPositionAnimation(this);

            mAnimateToStartPosition = new ToStartPositionAnimate(this);

        }

        protected override int GetChildDrawingOrder(int childCount, int i)
        {
            
            if (mHeaderViewIndex < 0 && mFooterViewIndex < 0)
            {
                return i;
            }
            if (i == childCount - 2)
            {
                return mHeaderViewIndex;
            }
            if (i == childCount - 1)
            {
                return mFooterViewIndex;
            }
            int bigIndex = mFooterViewIndex > mHeaderViewIndex ? mFooterViewIndex
                    : mHeaderViewIndex;
            int smallIndex = mFooterViewIndex < mHeaderViewIndex ? mFooterViewIndex
                    : mHeaderViewIndex;
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
            RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams(
                (int)(mHeaderViewHeight * 0.8),
                (int)(mHeaderViewHeight * 0.8));
            layoutParams.AddRule(LayoutRules.CenterHorizontal);
            layoutParams.AddRule(LayoutRules.AlignParentBottom);
            mHeadViewContainer = new HeadViewContainer(Context);
            mHeadViewContainer.Visibility = ViewStates.Gone;
            defaultProgressView.Visibility = ViewStates.Visible;
            defaultProgressView.SetOnDraw(false);
            mHeadViewContainer.AddView(defaultProgressView, layoutParams);
            AddView(mHeadViewContainer);
        }
        private void CreateFooterViewContainer()
        {
            mFooterViewContainer = new RelativeLayout(Context);
            mFooterViewContainer.Visibility = ViewStates.Gone;
            AddView(mFooterViewContainer);
        }


        public void SetOnPullRefreshListener(IOnPullRefreshListener listener)
        {
            mListener = listener;
        }

        public void SetHeaderViewBackgroundColor(int color)
        {
            mHeadViewContainer.SetBackgroundColor(new Color(color));
        }
        public void SetOnPushLoadMoreListener(IOnPushLoadMoreListener onPushLoadMoreListener)
        {
            mOnPushLoadMoreListener = onPushLoadMoreListener;
        }
        public void SetRefreshing(bool refreshing)
        {
            if (refreshing && mRefreshing != refreshing)
            {
                // scale and show
                mRefreshing = refreshing;
                int endTarget = 0;
                if (!mUsingCustomStart)
                {
                    endTarget = (int)(mSpinnerFinalOffset + mOriginalOffsetTop);
                }
                else
                {
                    endTarget = (int)mSpinnerFinalOffset;
                }
                SetTargetOffsetTopAndBottom(endTarget - mCurrentTargetOffsetTop, true /* requires update */);
                mNotify = false;
                StartScaleUpAnimation(mRefreshAnimationListener);
            }
            else
            {
                SetRefreshing(refreshing, false /* notify */);
                if (usingDefaultHeader)
                {
                    defaultProgressView.SetOnDraw(false);
                }
            }
        }

        private void StartScaleUpAnimation(Animation.IAnimationListener listener)
        {
            mHeadViewContainer.Visibility = ViewStates.Visible;

            mScaleAnimation = new ScaleAnimation(this);
            //實作類別請參考ScaleAnimation

            mScaleAnimation.Duration = mMediumAnimationDuration;
            if (listener != null)
            {
                mHeadViewContainer.SetAnimationListener(listener);
            }
            mHeadViewContainer.ClearAnimation();
            mHeadViewContainer.StartAnimation(mScaleAnimation);
        }
        private void SetAnimationProgress(float progress)
        {
            if (!usingDefaultHeader)
            {
                progress = 1;
            }
            ViewCompat.SetScaleX(mHeadViewContainer, progress);
            ViewCompat.SetScaleY(mHeadViewContainer, progress);
        }
        private void SetRefreshing(bool refreshing, bool notify)
        {

            if (mRefreshing != refreshing)
            {
                mNotify = notify;
                EnsureTarget();
                mRefreshing = refreshing;
                if (mRefreshing)
                {
                    AnimateOffsetToCorrectPosition(mCurrentTargetOffsetTop, mRefreshAnimationListener);
                }
                else
                {
                    StartScaleDownAnimation(mRefreshAnimationListener);
                }
            }
        }
        private void StartScaleDownAnimation(Animation.IAnimationListener listener)
        {
            mScaleDownAnimation = new ScaleDownAnimation(this)
            {
                Duration = SCALE_DOWN_DURATION
            };
            //實作類別請參考ScaleDownAnimation

            mHeadViewContainer.SetAnimationListener(listener);
            mHeadViewContainer.ClearAnimation();
            mHeadViewContainer.StartAnimation(mScaleDownAnimation);
        }

        public bool IsRefreshing()
        {
            return mRefreshing;
        }
        private void EnsureTarget()
        {
            if (mTarget != null) return;
            for (int i = 0; i < ChildCount; i++)
            {
                View child = this.GetChildAt(i);


                if (!child.Equals(mHeadViewContainer)
                    && !child.Equals(mFooterViewContainer))
                {
                    mTarget = child;
                    break;
                }
            }
        }

        public void SetDistanceToTriggerSync(int distance)
        {
            mTotalDragDistance = distance;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            int width = MeasuredWidth;
            int height = MeasuredHeight;
            if (ChildCount == 0)
            {
                return;
            }
            if (mTarget == null)
            {
                EnsureTarget();
            }
            if (mTarget == null)
            {
                return;
            }
            int distance = mCurrentTargetOffsetTop + mHeadViewContainer.Height;
            if (!targetScrollWithLayout)
            {

                distance = 0;
            }
            View child = mTarget;
            int childLeft = PaddingLeft;
            int childTop = PaddingTop + distance - pushDistance;
            int childWidth = width - PaddingLeft - PaddingRight;
            int childHeight = height - PaddingTop - PaddingBottom;
            Log.Debug(LOG_TAG, "debug:onLayout childHeight = " + childHeight);
            child.Layout(childLeft, childTop, childLeft + childWidth, childTop
                                                                      + childHeight);
            int headViewWidth = mHeadViewContainer.MeasuredWidth;
            int headViewHeight = mHeadViewContainer.MeasuredHeight;
            mHeadViewContainer.Layout((width/2 - headViewWidth/2),
                mCurrentTargetOffsetTop, (width/2 + headViewWidth/2),
                mCurrentTargetOffsetTop + headViewHeight);
            int footViewWidth = mFooterViewContainer.MeasuredWidth;
            int footViewHeight = mFooterViewContainer.MeasuredHeight;
            mFooterViewContainer.Layout((width/2 - footViewWidth/2), height
                                                                     - pushDistance, (width/2 + footViewWidth/2), height
                                                                                                                  +
                                                                                                                  footViewHeight -
                                                                                                                  pushDistance);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            if (mTarget == null)
            {
                EnsureTarget();
            }
            if (mTarget == null)
            {
                return;
            }
            mTarget.Measure(MeasureSpec.MakeMeasureSpec(MeasuredWidth
                                                        - PaddingLeft - PaddingRight, MeasureSpecMode.Exactly),
                MeasureSpec.MakeMeasureSpec(MeasuredHeight
                                            - PaddingTop - PaddingBottom,
                    MeasureSpecMode.Exactly));
            mHeadViewContainer.Measure(MeasureSpec.MakeMeasureSpec(
                mHeaderViewWidth, MeasureSpecMode.Exactly), MeasureSpec
                    .MakeMeasureSpec(3*mHeaderViewHeight, MeasureSpecMode.Exactly));
            mFooterViewContainer.Measure(MeasureSpec.MakeMeasureSpec(
                mFooterViewWidth, MeasureSpecMode.Exactly), MeasureSpec
                    .MakeMeasureSpec(mFooterViewHeight, MeasureSpecMode.Exactly));
            if (!mUsingCustomStart && !mOriginalOffsetCalculated)
            {
                mOriginalOffsetCalculated = true;
                mCurrentTargetOffsetTop = mOriginalOffsetTop = -mHeadViewContainer
                    .MeasuredHeight;
                UpdateListenerCallBack();
            }
            mHeaderViewIndex = -1;
            for (int index = 0; index < ChildCount; index++)
            {
                if (GetChildAt(index) == mHeadViewContainer)
                {
                    mHeaderViewIndex = index;
                    break;
                }
            }
            mFooterViewIndex = -1;
            for (int index = 0; index < ChildCount; index++)
            {

                if (GetChildAt(index) == mFooterViewContainer)
                {
                    mFooterViewIndex = index;
                    break;
                }
            }
        }

        public bool IsChildScrollToTop()
        {
            if (Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.IceCreamSandwich)
            {
                if (mTarget is AbsListView)
                {
                    AbsListView absListView = (AbsListView) mTarget;
                    return !(absListView.ChildCount > 0 && (absListView
                        .FirstVisiblePosition > 0 || absListView
                            .GetChildAt(0).Top < absListView.PaddingTop));
                }
                else
                {
                    return !(mTarget.ScrollY > 0);
                }
            }
            else
            {
                return !ViewCompat.CanScrollVertically(mTarget, -1);
            }
        }


        public bool IsChildScrollToBottom()
        {
            if (IsChildScrollToTop())
            {
                return false;
            }
            if (mTarget is RecyclerView)
            {
                RecyclerView recyclerView = (RecyclerView) mTarget;
                RecyclerView.LayoutManager layoutManager = recyclerView.GetLayoutManager();
                int count = recyclerView.GetAdapter().ItemCount;
                if (layoutManager is LinearLayoutManager && count > 0)
                {
                    LinearLayoutManager linearLayoutManager = (LinearLayoutManager) layoutManager;
                    if (linearLayoutManager.FindLastCompletelyVisibleItemPosition() == count - 1)
                    {
                        return true;
                    }
                }
                else if (layoutManager is StaggeredGridLayoutManager)
                {
                    StaggeredGridLayoutManager staggeredGridLayoutManager = (StaggeredGridLayoutManager) layoutManager;
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
            else if (mTarget is AbsListView)
            {
                AbsListView absListView = (AbsListView) mTarget;
                int count = absListView.Adapter.Count;
                int fristPos = absListView.FirstVisiblePosition;
                if (fristPos == 0
                    && absListView.GetChildAt(0).Top >= absListView
                        .PaddingTop)
                {
                    return false;
                }
                int lastPos = absListView.LastVisiblePosition;
                if (lastPos > 0 && count > 0 && lastPos == count - 1)
                {
                    return true;
                }
                return false;
            }
            else if (mTarget is ScrollView)
            {
                ScrollView scrollView = (ScrollView) mTarget;
                View view = (View) scrollView
                    .GetChildAt(scrollView.ChildCount - 1);
                if (view != null)
                {
                    int diff = (view.Bottom - (scrollView.Height + scrollView
                        .ScrollY));
                    if (diff == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            EnsureTarget();
            
            int action = MotionEventCompat.GetActionMasked(ev);
            //var action = ev.ActionMasked;
            //var action = ev.Action;

            if (mReturningToStart && action == (int) MotionEventActions.Down)
            {
                mReturningToStart = false;
            }
            if (!Enabled || mReturningToStart || mRefreshing || mLoadMore
                || (!IsChildScrollToTop() && !IsChildScrollToBottom()))
            {

                return false;
            }

            switch (action)
            {
                case (int) MotionEventActions.Down:
                    SetTargetOffsetTopAndBottom(
                        mOriginalOffsetTop - mHeadViewContainer.Top, true);
                    mActivePointerId = MotionEventCompat.GetPointerId(ev, 0);
                    mIsBeingDragged = false;
                    float initialMotionY = GetMotionEventY(ev, mActivePointerId);
                    if (initialMotionY == -1)
                    {
                        return false;
                    }
                    mInitialMotionY = initialMotionY;
                    break;
                case (int) MotionEventActions.Move:
                    if (mActivePointerId == INVALID_POINTER)
                    {
                        Log.Error(LOG_TAG,
                            "Got ACTION_MOVE event but don't have an active pointer id.");
                        return false;
                    }

                    float y = GetMotionEventY(ev, mActivePointerId);
                    if (y == -1)
                    {
                        return false;
                    }
                    float yDiff = 0;
                    if (IsChildScrollToBottom())
                    {
                        yDiff = mInitialMotionY - y;
                        if (yDiff > mTouchSlop && !mIsBeingDragged)
                        {
                            mIsBeingDragged = true;
                        }
                    }
                    else
                    {
                        yDiff = y - mInitialMotionY;
                        if (yDiff > mTouchSlop && !mIsBeingDragged)
                        {
                            mIsBeingDragged = true;
                        }
                    }
                    break;

                case MotionEventCompat.ActionPointerUp:
                    OnSecondaryPointerUp(ev);
                    break;

                case (int) MotionEventActions.Up:
                case (int) MotionEventActions.Cancel:
                    mIsBeingDragged = false;
                    mActivePointerId = INVALID_POINTER;
                    break;
            }

            return mIsBeingDragged;
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

            if (mReturningToStart && action == (int) MotionEventActions.Down)
            {
                mReturningToStart = false;
            }
            if (!Enabled || mReturningToStart || (!IsChildScrollToTop() && !IsChildScrollToBottom()))
            {
                return false;
            }

            if (IsChildScrollToBottom())
            {
                return HandlerPushTouchEvent(ev, action);
            }
            else
            {
                return HandlerPullTouchEvent(ev, action);
            }
        }

        private bool HandlerPushTouchEvent(MotionEvent ev, int action)
        {
            switch (action)
            {
                case (int)MotionEventActions.Down:
                    mActivePointerId = MotionEventCompat.GetPointerId(ev, 0);
                    mIsBeingDragged = false;
                    Log.Debug(LOG_TAG, "debug:onTouchEvent ACTION_DOWN");
                    break;
                case (int)MotionEventActions.Move:
                    {
                        int pointerIndex = MotionEventCompat.FindPointerIndex(ev,
                                mActivePointerId);
                        if (pointerIndex < 0)
                        {
                            Log.Error(LOG_TAG,
                                    "Got ACTION_MOVE event but have an invalid active pointer id.");
                            return false;
                        }
                         float y = MotionEventCompat.GetY(ev, pointerIndex);
                         float overscrollBottom = (mInitialMotionY - y) * DRAG_RATE;
                        if (mIsBeingDragged)
                        {
                            pushDistance = (int)overscrollBottom;
                            UpdateFooterViewPosition();
                            if (mOnPushLoadMoreListener != null)
                            {
                                mOnPushLoadMoreListener
                                        .OnPushEnable(pushDistance >= mFooterViewHeight);
                            }
                        }
                        break;
                    }
                case MotionEventCompat.ActionPointerDown:
                    {
                        int index = MotionEventCompat.GetActionIndex(ev);
                        mActivePointerId = MotionEventCompat.GetPointerId(ev, index);
                        break;
                    }

                case MotionEventCompat.ActionPointerUp:
                    OnSecondaryPointerUp(ev);
                    break;

                case (int)MotionEventActions.Up:
                case (int)MotionEventActions.Cancel:
                    {
                        if (mActivePointerId == INVALID_POINTER)
                        {
                            if (action == (int)MotionEventActions.Up)
                            {
                                Log.Error(LOG_TAG,
                                        "Got ACTION_UP event but don't have an active pointer id.");
                            }
                            return false;
                        }
                        int pointerIndex = MotionEventCompat.FindPointerIndex(ev,
                          mActivePointerId);
                        float y = MotionEventCompat.GetY(ev, pointerIndex);
                        float overscrollBottom = (mInitialMotionY - y) * DRAG_RATE;
                        mIsBeingDragged = false;
                        mActivePointerId = INVALID_POINTER;
                        if (overscrollBottom < mFooterViewHeight
                                || mOnPushLoadMoreListener == null)
                        {
                            pushDistance = 0;
                        }
                        else
                        {
                            pushDistance = mFooterViewHeight;
                        }
                        if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Honeycomb)
                        {
                            UpdateFooterViewPosition();
                            if (pushDistance == mFooterViewHeight
                                    && mOnPushLoadMoreListener != null)
                            {
                                mLoadMore = true;
                                mOnPushLoadMoreListener.OnLoadMore();
                            }
                        }
                        else
                        {
                            AnimatorFooterToBottom((int)overscrollBottom, pushDistance);
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
                    mActivePointerId = MotionEventCompat.GetPointerId(ev, 0);
                    mIsBeingDragged = false;
                    break;

                case (int) MotionEventActions.Move:
                {
                    int pointerIndex = MotionEventCompat.FindPointerIndex(ev, mActivePointerId);
                    if (pointerIndex < 0)
                    {
                        Log.Error(LOG_TAG,
                            "Got ACTION_MOVE event but have an invalid active pointer id.");
                        return false;
                    }

                    float y = MotionEventCompat.GetY(ev, pointerIndex);
                    float overscrollTop = (y - mInitialMotionY)*DRAG_RATE;
                    if (mIsBeingDragged)
                    {
                        float originalDragPercent = overscrollTop/mTotalDragDistance;
                        if (originalDragPercent < 0)
                        {
                            return false;
                        }
                        float dragPercent = System.Math.Min(1f, System.Math.Abs(originalDragPercent));
                        float extraOs = System.Math.Abs(overscrollTop) - mTotalDragDistance;
                        float slingshotDist = mUsingCustomStart ? mSpinnerFinalOffset - mOriginalOffsetTop : mSpinnerFinalOffset;
                        float tensionSlingshotPercent = System.Math.Max(0,
                            System.Math.Min(extraOs, slingshotDist*2)/slingshotDist);
                        float tensionPercent = (float) ((tensionSlingshotPercent/4) - System.Math
                            .Pow((tensionSlingshotPercent/4), 2))*2f;
                        float extraMove = (slingshotDist)*tensionPercent*2;

                        int targetY = mOriginalOffsetTop
                                      + (int) ((slingshotDist*dragPercent) + extraMove);
                        if (mHeadViewContainer.Visibility != ViewStates.Visible)
                        {
                            mHeadViewContainer.Visibility = ViewStates.Visible;
                        }
                        if (!mScale)
                        {
                            ViewCompat.SetScaleX(mHeadViewContainer, 1f);
                            ViewCompat.SetScaleY(mHeadViewContainer, 1f);
                        }
                        if (usingDefaultHeader)
                        {
                            float alpha = overscrollTop/mTotalDragDistance;
                            if (alpha >= 1.0f)
                            {
                                alpha = 1.0f;
                            }
                            ViewCompat.SetScaleX(defaultProgressView, alpha);
                            ViewCompat.SetScaleY(defaultProgressView, alpha);
                            ViewCompat.SetAlpha(defaultProgressView, alpha);
                        }
                        if (overscrollTop < mTotalDragDistance)
                        {
                            if (mScale)
                            {
                                SetAnimationProgress(overscrollTop/mTotalDragDistance);
                            }
                            if (mListener != null)
                            {
                                mListener.OnPullEnable(false);
                            }
                        }
                        else
                        {
                            if (mListener != null)
                            {
                                mListener.OnPullEnable(true);
                            }
                        }
                        SetTargetOffsetTopAndBottom(targetY - mCurrentTargetOffsetTop, true);
                    }
                    break;
                }
                case MotionEventCompat.ActionPointerDown:
                {
                    int index = MotionEventCompat.GetActionIndex(ev);
                    mActivePointerId = MotionEventCompat.GetPointerId(ev, index);
                    break;
                }

                case MotionEventCompat.ActionPointerUp:
                    OnSecondaryPointerUp(ev);
                    break;

                case (int) MotionEventActions.Up:
                case (int) MotionEventActions.Cancel:
                {
                    if (mActivePointerId == INVALID_POINTER)
                    {
                        if (action == (int) MotionEventActions.Up)
                        {
                            Log.Error(LOG_TAG,
                                "Got ACTION_UP event but don't have an active pointer id.");
                        }
                        return false;
                    }
                    int pointerIndex = MotionEventCompat.FindPointerIndex(ev,
                        mActivePointerId);
                    float y = MotionEventCompat.GetY(ev, pointerIndex);
                    float overscrollTop = (y - mInitialMotionY)*DRAG_RATE;
                    mIsBeingDragged = false;
                    if (overscrollTop > mTotalDragDistance)
                    {
                        SetRefreshing(true, true /* notify */);
                    }
                    else
                    {
                        mRefreshing = false;
                        Animation.IAnimationListener listener = null;
                        if (!mScale)
                        {
                            listener = new ScaleAnimationListener(this);
                            //請參考ScaleAnimationListener實作Animation.IAnimationListener
                        }

                        AnimateOffsetToStartPosition(mCurrentTargetOffsetTop, listener);
                    }
                }
                    mActivePointerId = INVALID_POINTER;
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
            ValueAnimator valueAnimator = ValueAnimator.OfInt(start, end);
            valueAnimator.SetDuration(50);

            valueAnimator.Update += (sender, args) =>
            {

                // update
                pushDistance = (int)valueAnimator.AnimatedValue;
                UpdateFooterViewPosition();
            };
            valueAnimator.AnimationEnd += (sender, args) =>
            {
                if (end > 0 && mOnPushLoadMoreListener != null)
                {
                    // start loading more
                    mLoadMore = true;
                    mOnPushLoadMoreListener.OnLoadMore();
                }
                else
                {
                    ResetTargetLayout();
                    mLoadMore = false;
                }

            };

            valueAnimator.SetInterpolator(mDecelerateInterpolator);
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
                mFrom = from;
                mAnimateToStartPosition.Reset();
                mAnimateToStartPosition.Duration = ANIMATE_TO_START_DURATION;
                mAnimateToStartPosition.Interpolator = mDecelerateInterpolator;
                if (listener != null)
                {
                    mHeadViewContainer.SetAnimationListener(listener);
                }
                mHeadViewContainer.ClearAnimation();
                mHeadViewContainer.StartAnimation(mAnimateToStartPosition);
            }
            ResetTargetLayoutDelay(ANIMATE_TO_START_DURATION);
        }

        public void SetLoadMore(bool loadMore)
        {
            if (!loadMore && mLoadMore)
            {
                if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Honeycomb)
                {
                    mLoadMore = false;
                    pushDistance = 0;
                    UpdateFooterViewPosition();
                }
                else
                {
                    AnimatorFooterToBottom(mFooterViewHeight, 0);
                }
            }
        }

        private void AnimateOffsetToCorrectPosition(int from, Animation.IAnimationListener listener)
        {
            mFrom = from;
            mAnimateToCorrectPosition.Reset();
            mAnimateToCorrectPosition.Duration = ANIMATE_TO_TRIGGER_DURATION;
            mAnimateToCorrectPosition.Interpolator = mDecelerateInterpolator;
            if (listener != null)
            {
                mHeadViewContainer.SetAnimationListener(listener);
            }
            mHeadViewContainer.ClearAnimation();
            mHeadViewContainer.StartAnimation(mAnimateToCorrectPosition);
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
            View child = mTarget;
            int childLeft = PaddingLeft;
            int childTop = PaddingTop;
            int childWidth = child.Width - PaddingLeft
                             - PaddingRight;
            int childHeight = child.Height - PaddingTop
                              - PaddingBottom;
            child.Layout(childLeft, childTop, childLeft + childWidth, childTop
                                                                      + childHeight);

            int headViewWidth = mHeadViewContainer.MeasuredWidth;
            int headViewHeight = mHeadViewContainer.MeasuredHeight;
            mHeadViewContainer.Layout((width/2 - headViewWidth/2),
                -headViewHeight, (width/2 + headViewWidth/2), 0);
            int footViewWidth = mFooterViewContainer.MeasuredWidth;
            int footViewHeight = mFooterViewContainer.MeasuredHeight;
            mFooterViewContainer.Layout((width/2 - footViewWidth/2), height,
                (width/2 + footViewWidth/2), height + footViewHeight);
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
                    endTarget = (int)(_ssrl.mSpinnerFinalOffset - System.Math
                        .Abs(_ssrl.mOriginalOffsetTop));
                }
                else
                {
                    endTarget = (int)_ssrl.mSpinnerFinalOffset;
                }
                targetTop = (_ssrl.mFrom + (int)((endTarget - _ssrl.mFrom) * interpolatedTime));
                int offset = targetTop - _ssrl.mHeadViewContainer.Top;
                _ssrl.SetTargetOffsetTopAndBottom(offset, false /* requires update */);
            }

            public override void SetAnimationListener(IAnimationListener listener)
            {
                base.SetAnimationListener(listener);
            }


        }

        private void MoveToStart(float interpolatedTime)
        {
            int targetTop = 0;
            targetTop = (mFrom + (int)((mOriginalOffsetTop - mFrom) * interpolatedTime));
            int offset = targetTop - mHeadViewContainer.Top;
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
            mFrom = from;
            mStartingScale = ViewCompat.GetScaleX(mHeadViewContainer);
            mScaleDownToStartAnimation = new ScaleDownToStartAnimation(this);
            //實作類別請參考ScaleDownToStartAnimation

            mScaleDownToStartAnimation.Duration = SCALE_DOWN_DURATION;
            if (listener != null)
            {
                mHeadViewContainer.SetAnimationListener(listener);
            }
            mHeadViewContainer.ClearAnimation();
            mHeadViewContainer.StartAnimation(mScaleDownToStartAnimation);
        }

        private void SetTargetOffsetTopAndBottom(int offset, bool requiresUpdate)
        {
            mHeadViewContainer.BringToFront();
            mHeadViewContainer.OffsetTopAndBottom(offset);
            mCurrentTargetOffsetTop = mHeadViewContainer.Top;
            if (requiresUpdate && Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Honeycomb)
            {
                Invalidate();
            }
            UpdateListenerCallBack();
        }

        private void UpdateFooterViewPosition()
        {
            mFooterViewContainer.Visibility = ViewStates.Visible;
            mFooterViewContainer.BringToFront();
            mFooterViewContainer.OffsetTopAndBottom(-pushDistance);
            UpdatePushDistanceListener();
        }

        private void UpdatePushDistanceListener()
        {

            if (mOnPushLoadMoreListener != null)
            {
                mOnPushLoadMoreListener.OnPushDistance(pushDistance);
            }
        }

        private void OnSecondaryPointerUp(MotionEvent ev)
        {
            int pointerIndex = MotionEventCompat.GetActionIndex(ev);
            int pointerId = MotionEventCompat.GetPointerId(ev, pointerIndex);

            
            if (pointerId == mActivePointerId)
            {
                int newPointerIndex = pointerIndex == 0 ? 1 : 0;
                mActivePointerId = MotionEventCompat.GetPointerId(ev,
                    newPointerIndex);
            }
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

                //if (_mAnimationListener != null)
                //{
                //    _mAnimationListener.OnAnimationEnd(Animation);
                //}
            }
        }
        
        public bool IsTargetScrollWithLayout()
        {
            return targetScrollWithLayout;
        }

        public void SetTargetScrollWithLayout(bool targetScrollWithLayout)
        {
            this.targetScrollWithLayout = targetScrollWithLayout;
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

        public class OnPullRefreshListenerAdapter : IOnPullRefreshListener
        {
            void IOnPullRefreshListener.OnRefresh()
            {
                
            }

            void IOnPullRefreshListener.OnPullDistance(int distance)
            {
                
            }

            void IOnPullRefreshListener.OnPullEnable(bool enable)
            {
                
            }
        }

        public class OnPushLoadMoreListenerAdapter : IOnPushLoadMoreListener
        {
            void IOnPushLoadMoreListener.OnLoadMore()
            {
                
            }

            void IOnPushLoadMoreListener.OnPushDistance(int distance)
            {
               
            }

            void IOnPushLoadMoreListener.OnPushEnable(bool enable)
            {
               
            }
        }

        public void SetDefaultCircleProgressColor(uint color)
        {
            if (usingDefaultHeader)
            {
                defaultProgressView.SetProgressColor(color);
            }
        }


        public void SetDefaultCircleBackgroundColor(uint color)
        {
            if (usingDefaultHeader)
            {
                defaultProgressView.SetCircleBackgroundColor(color);
            }
        }

        public void SetDefaultCircleShadowColor(uint color)
        {
            if (usingDefaultHeader)
            {
                defaultProgressView.SetShadowColor(color);
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
                if (bgRect == null)
                {
                    int offset = (int) (density*2);
                    bgRect = new RectF(offset, offset, _width - offset, _height
                                                                        - offset);
                }
                return bgRect;
            }

            private RectF GetOvalRect()
            {
                _width = Width;
                _height = Height;
                if (ovalRect == null)
                {
                    int offset = (int) (density*8);
                    ovalRect = new RectF(offset, offset, _width - offset, _height
                                                                          - offset);
                }
                return ovalRect;
            }

            public void SetProgressColor(uint progressColor)
            {
                this._progressColor = progressColor;
            }

            public void SetCircleBackgroundColor(uint circleBackgroundColor)
            {
                this._circleBackgroundColor = circleBackgroundColor;
            }

            public void SetShadowColor(uint shadowColor)
            {
                this._shadowColor = shadowColor;
            }


            private Paint CreatePaint()
            {
                if (this._progressPaint == null)
                {
                    _progressPaint = new Paint();
                    _progressPaint.StrokeWidth = (int) (density*3);
                    _progressPaint.SetStyle(Paint.Style.Stroke);
                    _progressPaint.AntiAlias = true;
                }
                _progressPaint.Color = new Color((int) _progressColor);
                return _progressPaint;
            }

            private Paint CreateBgPaint()
            {
                if (this._bgPaint == null)
                {
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
                }
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
                    if (time < PEROID)
                    {
                        try
                        {
                            Thread.Sleep(PEROID - time);
                        }
                        catch (InterruptedException e)
                        {
                            e.PrintStackTrace();
                        }
                    }
                }
            }

            public void SetOnDraw(bool isOnDraw)
            {
                this._isOnDraw = isOnDraw;
            }

            public void SetSpeed(int speed)
            {
                this._speed = speed;
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
                float targetScale = (_ssrl.mStartingScale + (-_ssrl.mStartingScale * interpolatedTime));
                _ssrl.SetAnimationProgress(targetScale);
                _ssrl.MoveToStart(interpolatedTime);
            }

        }
    }

}