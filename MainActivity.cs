using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using DittoSDK;
using Java.Lang;
using static AndroidX.RecyclerView.Widget.RecyclerView;
#pragma warning disable CS0618 // Type or member is obsolete

namespace DittoXamarinAndroidTasksApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.DittoXamarinAndroidTasksApp", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, NewTaskDialogListener
    {
        private RecyclerView recyclerView;
        private LayoutManager viewManager;
        private TasksAdapter viewAdapter;

        private Ditto ditto;
        private DittoCollection collection;
        private DittoLiveQuery liveQuery;
        private DittoSubscription subscription;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            viewManager = new LinearLayoutManager(this);
            TasksAdapter tasksAdapter = new TasksAdapter();
            viewAdapter = tasksAdapter;

            //setup tasks list 
            recyclerView = (RecyclerView)FindViewById(Resource.Id.recyclerView);
            recyclerView.HasFixedSize = true;
            recyclerView.SetLayoutManager(viewManager);
            recyclerView.SetAdapter(viewAdapter);
            recyclerView.AddItemDecoration(new DividerItemDecoration(this, DividerItemDecoration.Vertical));

            // Create an instance of Ditto
            var workingDir = $"{Xamarin.Essentials.FileSystem.AppDataDirectory}/ditto";
            this.ditto = new Ditto(DittoIdentity.OnlinePlayground("<ADD_YOUR_APP_ID>", "<ADD_YOUR_TOKEN>", true), workingDir);

            // With 4.5.0-alpha2 this needs to be manually disabled, othrwise the app will crash.
            // This will be resolved with a later release. 
            ditto.TransportConfig.PeerToPeer.BluetoothLE.Enabled = false;

            // This starts Ditto's background synchronization
            try
            {
                ditto.StartSync();
            }
            catch (DittoException e)
            {
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }

            // We will create a long-running live query to keep the database up-to-date
            this.collection = this.ditto.Store.Collection("tasks");
            this.subscription = this.collection.Find("!isDeleted").Subscribe();

            // Add swipe to delete
            MySwipeToDelete swipeHandler = new MySwipeToDelete(this, BaseContext!);
 

            // Configure the RecyclerView for swipe to delete
            ItemTouchHelper itemTouchHelper = new ItemTouchHelper(swipeHandler);
            itemTouchHelper.AttachToRecyclerView(recyclerView);

            //Respond to new task button click
            FindViewById(Resource.Id.addTaskButton).Click += (s, e) =>
            {
                ShowNewTaskUI();
            };


            tasksAdapter.OnItemClick += (s, e) =>
            {
                ditto.Store.Collection("tasks").FindById(e.DittoDocument.Id).Update((dittoMutableDocument) =>
                {
                    try
                    {
                        dittoMutableDocument["isCompleted"].Set(!dittoMutableDocument["isCompleted"].BooleanValue);
                    }
                    catch (DittoException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.StackTrace);
                    }
                });
            };

            // This function will create a "live-query" that will update
            // our RecyclerView
            SetupTaskList();
        }

        void SetupTaskList()
        {
            // We use observeLocal to create a live query with a subscription to sync this query with other devices
            this.liveQuery = collection.Find("!isDeleted").ObserveLocal(OnObserveLocalHandler);

            ditto.Store.Collection("tasks").Find("isDeleted == true").Evict();
        }


        public void OnObserveLocalHandler(IList<DittoDocument> docs, DittoLiveQueryEvent e)
        {
            TasksAdapter adapter = this.viewAdapter;
            if (e is DittoLiveQueryEvent.Update updateEvent)
            {
                RunOnUiThread(() =>
                {
                    adapter.SetTasks(docs);
                    adapter.Inserts(updateEvent.Insertions.Cast<int>().ToList());
                    adapter.Deletes(updateEvent.Deletions.Cast<int>().ToList());
                    adapter.Updates(updateEvent.Updates.Cast<int>().ToList());
                    adapter.Moves(updateEvent.Moves.Cast<DittoLiveQueryMove>().ToList());
                });
            }
            else if (e is DittoLiveQueryEvent.Initial)
            {
                RunOnUiThread(() =>
                {
                    adapter.SetInitial(docs);
                });
            }
        }

        void ShowNewTaskUI()
        {
            NewTaskDialogFragment newFragment = new NewTaskDialogFragment(Resource.String.add_new_task_dialog_title);
            newFragment.Show(FragmentManager, "newTask");
        }

        public void OnDialogSave(DialogFragment dialog, string task)
        {
            var map = new Dictionary<string, object>
            {
                { "body", task },
                { "isCompleted", false },
                { "isDeleted", false }
            };
            collection.Upsert(map);
        }

        public void OnDialogCancel(DialogFragment dialog)
        {
        }


        public class MySwipeToDelete : SwipeToDeleteCallback
        {
            private MainActivity mainActivity1;
            public MySwipeToDelete(MainActivity mainActivity, Context context) : base(context)
            {
                mainActivity1 = mainActivity;
            }

            public override void OnSwiped(ViewHolder viewHolder, int p1)
            {
                TasksAdapter adapter = (TasksAdapter)mainActivity1.recyclerView.GetAdapter();
                // Retrieve the task at the row swiped
                DittoDocument task = adapter.GetTasks().ElementAt(viewHolder.AdapterPosition);
                // Delete the task from Ditto
                mainActivity1.ditto.Store.Collection("tasks").FindById(task.Id).Update(doc =>
                {
                    try
                    {
                        doc["isDeleted"].Set(true);
                    }
                    catch (DittoException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.StackTrace);
                    }
                });
            }
        }

        public abstract class SwipeToDeleteCallback : ItemTouchHelper.SimpleCallback
        {

            private Context context;
            private Drawable deleteIcon;
            private int intrinsicWidth;
            private int intrinsicHeight;
            private ColorDrawable background = new ColorDrawable();
            private int backgroundColor = Color.ParseColor("#f44336");
            private Paint clearPaint = new Paint();

            public SwipeToDeleteCallback(Context context) : base(0, ItemTouchHelper.Left)
            {
                this.context = context;
                deleteIcon = ContextCompat.GetDrawable(this.context, Android.Resource.Drawable.IcMenuDelete);
                intrinsicWidth = deleteIcon.IntrinsicWidth;
                intrinsicHeight = deleteIcon.IntrinsicHeight;
                clearPaint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.Clear));
            }

            public override bool OnMove(RecyclerView p0, RecyclerView.ViewHolder p1, RecyclerView.ViewHolder p2)
            {
                return false;
            }

            public override void OnChildDraw(Canvas c, RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, float dX, float dY, int actionState, bool isCurrentlyActive)
            {
                View itemView = viewHolder.ItemView;
                int itemHeight = itemView.Bottom - itemView.Top;
                bool isCanceled = dX == 0f && !isCurrentlyActive;

                if (isCanceled)
                {
                    clearCanvas(c, new Float(itemView.Right + dX), new Float(itemView.Top), itemView.Right, new Float(itemView.Bottom));
                    base.OnChildDraw(c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive);
                    return;
                }

                // Draw the red delete background
                background.Color = Color.Red;
                background.SetBounds(itemView.Right + (int)dX, itemView.Top, itemView.Right, itemView.Bottom);
                background.Draw(c);

                // Calculate position of delete icon
                int deleteIconTop = itemView.Top + (itemHeight - intrinsicHeight) / 2;
                int deleteIconMargin = (itemHeight - intrinsicHeight) / 2;
                int deleteIconLeft = itemView.Right - deleteIconMargin - intrinsicWidth;
                int deleteIconRight = itemView.Right - deleteIconMargin;
                int deleteIconBottom = deleteIconTop + intrinsicHeight;

                // Draw the delete icon
                deleteIcon.SetBounds(deleteIconLeft, deleteIconTop, deleteIconRight, deleteIconBottom);
                deleteIcon.SetTint(Color.ParseColor("#ffffff"));
                deleteIcon.Draw(c);

                base.OnChildDraw(c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive);
            }

            private void clearCanvas(Canvas c, Float left, Float top, float right, Float bottom)
            {
                c.DrawRect(left.FloatValue(), top.FloatValue(), right, bottom.FloatValue(), clearPaint);
            }
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete