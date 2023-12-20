using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace DittoXamarinAndroidTasksApp
{
    public class TasksAdapterItemClickEventArgs : EventArgs
    {
        public DittoTask DittoTask { get; set; }
    }

    public class MyViewHolder : RecyclerView.ViewHolder
    {
        public MyViewHolder(View itemView) : base(itemView)
        {
        }
    }

    public class TasksAdapter : RecyclerView.Adapter
    {
        private List<DittoTask> tasks = new List<DittoTask>();

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
            DittoTask task = tasks.ElementAt(position);
            ((TextView)holder.ItemView.FindViewById(Resource.Id.taskTextView)).Text = task.Body;
            ((CheckBox)holder.ItemView.FindViewById(Resource.Id.taskCheckBox)).Checked = task.IsCompleted;

            holder.ItemView.Click += ItemClickHandler;

            void ItemClickHandler(object sender, EventArgs e)
            {
                OnItemClick?.Invoke(this, new TasksAdapterItemClickEventArgs()
                {
                    DittoTask = tasks.ElementAt(holder.AdapterPosition)
                });
                holder.ItemView.Click -= ItemClickHandler;
            }
        }

        #endregion

        public override int ItemCount => tasks.Count;

        public List<DittoTask> GetTasks()
        {
            return tasks;
        }

        public int SetTasks(IList<DittoTask> newTasks)
        {
            this.tasks.Clear();
            this.tasks.AddRange(newTasks);
            this.NotifyDataSetChanged();

            return tasks.Count;
        }
    }
}
