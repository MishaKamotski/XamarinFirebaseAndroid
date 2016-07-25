using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Gms.Auth.Api;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common;
using Android.Gms.Tasks;
using Android.Gms.Common.Apis;
using Android.Views;
using Android.OS;
using Android.Support.V4.App;
using Firebase.Auth;
using Firebase;
using Firebase.Database;
using Org.Json;

namespace Xamarin.Firebase.Android
{
    [Activity(Label = "Xamarin.Firebase.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : FragmentActivity, GoogleApiClient.IOnConnectionFailedListener, IOnFailureListener,
        View.IOnClickListener, IValueEventListener, IOnCompleteListener
    {
        private static int RC_SIGN_IN = 9001;
        private GoogleApiClient mGoogleApiClient;
        private FirebaseAuth mAuth;
        private FirebaseApp firebaseApp;
        private DatabaseReference mDatabase;
        private FirebaseDatabase database;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            var options = new FirebaseOptions.Builder()
                .SetApiKey(GetString(Resource.String.ApiKey))
                .SetApplicationId(GetString(Resource.String.ApplicationId))
                .SetDatabaseUrl(GetString(Resource.String.DatabaseUrl))
                .Build();

            firebaseApp = FirebaseApp.InitializeApp(this, options, Application.PackageName);

            GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                    .RequestIdToken(GetString(Resource.String.ServerClientId))
                    .RequestId()
                    .RequestEmail()
                    .Build();

            mGoogleApiClient = new GoogleApiClient.Builder(this)
               .EnableAutoManage(this, this)
               .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
               .Build();

            mAuth = FirebaseAuth.GetInstance(firebaseApp);
            var db = FirebaseDatabase.GetInstance(firebaseApp);
            mDatabase = db.GetReference("/Info");
            mDatabase.AddListenerForSingleValueEvent(this);

            SignInButton signInButton = (SignInButton)FindViewById(Android.Resource.Id.sign_in_button);
            signInButton.SetSize(SignInButton.SizeStandard);
            signInButton.SetScopes(gso.GetScopeArray());
            FindViewById(Android.Resource.Id.sign_in_button).SetOnClickListener(this);
        }


        public void OnConnectionFailed(ConnectionResult result)
        {
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.sign_in_button:
                    signIn();
                    break;
            }
        }

        private void signIn()
        {
            Intent signInIntent = Auth.GoogleSignInApi.GetSignInIntent(mGoogleApiClient);
            StartActivityForResult(signInIntent, RC_SIGN_IN);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == RC_SIGN_IN)
            {
                GoogleSignInResult result = Auth.GoogleSignInApi.GetSignInResultFromIntent(data);
                if (result.IsSuccess)
                {
                    // Google Sign In was successful, authenticate with Firebase
                    GoogleSignInAccount account = result.SignInAccount;
                    firebaseAuthWithGoogle(account);
                }
            }
        }

        private void firebaseAuthWithGoogle(GoogleSignInAccount acct)
        {
            AuthCredential credential = GoogleAuthProvider.GetCredential(acct.IdToken, null);
            
            mAuth.SignInWithCredential(credential).AddOnFailureListener(this, this).AddOnCompleteListener(this, this);
        }

        public void OnFailure(Java.Lang.Exception e)
        {
        }

        public void OnCancelled(DatabaseError error)
        {
        }
        //https://forums.xamarin.com/discussion/70248/firebase-configuration/p2
        public void OnDataChange(DataSnapshot snapshot)
        {
            if (snapshot.Key == "Info")
            {
              
            }
            else
            {

            }
        }

        public void OnComplete(Task task)
        {
            var db = FirebaseDatabase.GetInstance(firebaseApp);
            mDatabase = db.GetReference("/Katya");
            mDatabase.AddListenerForSingleValueEvent(this);
        }
    }

    //https://github.com/firebase/Firebase-Unity/issues/40
    static class JsonHelper 
    {
        static Type stringType = typeof(System.String);
        static Type dictionaryType = typeof(Dictionary<string, object>);
        static Type boolType = typeof(System.Boolean);

        static JSONObject ToJson(Dictionary<string, object> dictionary)
        {
            return ToJson(dictionary, new JSONObject());
        }

        static JSONObject ToJson(Dictionary<string, object> dictionary, JSONObject root)
        {
            foreach (string key in dictionary.Keys)
            {
                Type type = dictionary[key].GetType();

                // Dictionary/map
                if (type == dictionaryType)
                {
                    JSONObject innerDictionary = ToJson((Dictionary<string, object>)dictionary[key], new JSONObject());
                    root.Put(key, innerDictionary);
                }

                // String
                else if (type == stringType)
                {
                    root.Put(key, dictionary[key].ToString());
                }

                // Boolean
                else if (type == boolType)
                {
                    root.Put(key, (bool)dictionary[key]);
                }

                // Numeric
                else
                {
                    root.Put(key, float.Parse(dictionary[key].ToString()));
                }

            }

            return root;
        }
    }

}