using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using DittoSDK;
using Java.Lang;

namespace DittoXamarinAndroidTasksApp
{
    public class TasksAdapterItemClickEventArgs : EventArgs
    {
        public DittoDocument DittoDocument { get; set; }
    }

    public class MyViewHolder : RecyclerView.ViewHolder
    {
        public MyViewHolder(View itemView) : base(itemView)
        {
        }
    }

    public class TasksAdapter : RecyclerView.Adapter
    {
        private List<DittoDocument> tasks = new List<DittoDocument>();

        public event EventHandler<TasksAdapterItemClickEventArgs> OnItemClick;

        public TasksAdapter()
        {
        }

        #region Lifecycle
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.task_view, parent, false);

            return new MyViewHolder(itemView: view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            DittoDocument task = tasks.ElementAt(position);
            ((TextView)holder.ItemView.FindViewById(Resource.Id.taskTextView)).Text = task["body"].StringValue;
            ((CheckBox)holder.ItemView.FindViewById(Resource.Id.taskCheckBox)).Checked = task["isCompleted"].BooleanValue;

            holder.ItemView.Click += ItemClickHandler;

            void ItemClickHandler(object sender, EventArgs e)
            {
                OnItemClick?.Invoke(this, new TasksAdapterItemClickEventArgs()
                {
                    DittoDocument = tasks.ElementAt(holder.AdapterPosition)
                });
                holder.ItemView.Click -= ItemClickHandler;
            }
        }

        #endregion

        public override int ItemCount => tasks.Count;

        public List<DittoDocument> GetTasks()
        {
            return tasks;
        }

        public int SetTasks(IList<DittoDocument> newTasks)
        {
            this.tasks.Clear();
            this.tasks.AddRange(newTasks);
            return tasks.Count;
        }

        public int Inserts(IList<int> indexes)
        {
            foreach (int index in indexes)
            {
                this.NotifyItemRangeInserted(index, 1);
            }
            return tasks.Count;
        }

        public int Deletes(IList<int> indexes)
        {
            foreach (int index in indexes)
            {
                this.NotifyItemRangeRemoved(index, 1);
            }
            return this.tasks.Count;
        }

        public int Updates(IList<int> indexes)
        {
            foreach (int index in indexes)
            {
                this.NotifyItemRangeChanged(index, 1);
            }
            return this.tasks.Count;
        }

        public void Moves(IList<DittoLiveQueryMove> moves)
        {
            foreach (DittoLiveQueryMove move in moves)
            {
                this.NotifyItemMoved(move.From, move.To);
            }
        }

        public int SetInitial(IList<DittoDocument> initialTasks)
        {
            this.tasks.AddRange(initialTasks);
            this.NotifyDataSetChanged();
            return this.tasks.Count;
        }
    }
}

