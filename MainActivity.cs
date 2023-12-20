using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
using Newtonsoft.Json;
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

            SetupDitto();

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
                var updateQuery = $"UPDATE {DittoTask.CollectionName} " +
                    $"SET isCompleted = {!e.DittoTask.IsCompleted} " +
                    $"WHERE _id = '{e.DittoTask.Id}'";
                ditto.Store.ExecuteAsync(updateQuery);
            };

            SetupTaskList();
        }

        private void SetupDitto()
        {
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
        }

        void SetupTaskList()
        {
            var query = $"SELECT * FROM {DittoTask.CollectionName} WHERE isDeleted = false";

            ditto.Sync.RegisterSubscription(query);
            ditto.Store.RegisterObserver(query, storeObservationHandler: async (queryResult) =>
            {
                RunOnUiThread(() =>
                {
                    var adapter = this.viewAdapter;

                    adapter.SetTasks(queryResult.Items.ConvertAll(d =>
                    {
                        return JsonConvert.DeserializeObject<DittoTask>(d.JsonString());
                    }));
                }); 
            });

            ditto.Store.ExecuteAsync($"EVICT FROM {DittoTask.CollectionName} WHERE isDeleted = false");
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
            ditto.Store.ExecuteAsync($"INSERT INTO {DittoTask.CollectionName} DOCUMENTS (:doc1)", new Dictionary<string, object>()
            {
                { "doc1", map }
            });
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
                DittoTask task = adapter.GetTasks().ElementAt(viewHolder.AdapterPosition);
                // Delete the task from Ditto
                var updateQuery = $"UPDATE {DittoTask.CollectionName} " +
                    "SET isDeleted = true " +
                    $"WHERE _id = '{task.Id}'";
                mainActivity1.ditto.Store.ExecuteAsync(updateQuery);
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