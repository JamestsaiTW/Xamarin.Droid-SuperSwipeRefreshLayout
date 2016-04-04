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
using SuperSwipeRefreshLayoutDemoApp.ViewHolders;


namespace SuperSwipeRefreshLayoutDemoApp.Adapters
{
    public class RecyclerAdapter : RecyclerView.Adapter
    {
        private Context mContext;
        private List<String> mDataSet;

        public RecyclerAdapter(Context context)
        {
            mContext = context;
            mDataSet = new List<String>();
        }
        public override int ItemCount
        {
            get { return mDataSet.Count; }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            ChildViewHolder textViewHolder = (ChildViewHolder) holder;
            textViewHolder.BindView(mDataSet[position], position);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(mContext).Inflate(
                Resource.Layout.RecyclerView_Item, parent, false);
            return new ChildViewHolder(view);
        }

        protected void RemoveAll(int position, int itemCount)
        {
            for (int i = 0; i < itemCount; i++)
            {
                mDataSet.RemoveAt(position);
            }
            NotifyItemRangeRemoved(position, itemCount);
        }


        public override int GetItemViewType(int position)
        {
            return 0;
        }

        public void Add(String text, int position)
        {
            mDataSet.Insert(position, text);
            NotifyItemInserted(position);
        }

        public void AddAll(List<String> list, int position)
        {
            mDataSet.InsertRange(position, list);
            NotifyItemRangeInserted(position, list.Count);
        }

    }
}