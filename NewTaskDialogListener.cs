using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

#pragma warning disable CS0618, CS0672// Type or member is obsolete

namespace DittoXamarinAndroidTasksApp
{
    interface NewTaskDialogListener
    {
        void OnDialogSave(DialogFragment dialog, string task);

        void OnDialogCancel(DialogFragment dialog);
    }

    public class NewTaskDialogFragment : DialogFragment
    {
        NewTaskDialogListener newTaskDialogListener;

        public NewTaskDialogFragment(int title)
        {
            Bundle args = new Bundle();
            args.PutInt("dialog_title", title);
            Arguments = args;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {

            int title = Arguments.GetInt("dialog_title");
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity);
            builder.SetTitle(title);

            View dialogView = Activity.LayoutInflater.Inflate(Resource.Layout.dialog_new_task, null);
            TextView task = (TextView) dialogView.FindViewById(Resource.Id.editText);
            NewTaskDialogFragment instance = this;

            builder.SetView(dialogView)
                .SetPositiveButton(Resource.String.save, (s, e) =>
                {
                    if (newTaskDialogListener != null)
                    {
                        newTaskDialogListener.OnDialogSave(instance, task.Text);
                    }
                })
                .SetNegativeButton(Android.Resource.String.Cancel, (s, e) =>
                {
                    if (newTaskDialogListener != null)
                    {
                        newTaskDialogListener.OnDialogCancel(instance);
                    }
                });

            return builder.Create();
        }

        public override void OnAttach(Activity? activity)
        {
            base.OnAttach(activity);

            newTaskDialogListener = (NewTaskDialogListener)activity;
        }
    }
}

#pragma warning restore CS0618, CS0672 // Type or member is obsolete
