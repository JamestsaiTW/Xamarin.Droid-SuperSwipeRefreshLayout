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

namespace SuperSwipeRefreshLayoutDemoApp.ViewHolders
{
    public class ChildViewHolder: BaseViewHolder
    {
        private readonly TextView _textView;
        private readonly ImageView _imageView;

        public ChildViewHolder(View itemView):base(itemView)
        {

            _textView = (TextView)itemView.FindViewById(Resource.Id.text);
            _imageView = (ImageView)itemView.FindViewById(Resource.Id.image);
        }

        public void BindView(string str, int position)
        {
            _textView.Text = str;
        }
    }
}