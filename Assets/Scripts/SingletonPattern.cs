using UnityEngine;

namespace SingletonPattern
{
    public class Singleton<T> : MonoBehaviour where T:MonoBehaviour
    {
        private static volatile T instance = null;

        public static T GetInstance
        {
            get
            {
				if (instance == null) {
					GameObject obj;

					obj = GameObject.Find (typeof(T).Name);

					if (obj == null) {
						obj = new GameObject (typeof(T).Name);
						if(Application.isPlaying)
							DontDestroyOnLoad (obj);
						instance = obj.AddComponent<T> ();
					} else {
						instance = obj.GetComponent<T> ();
					}
				}

				return instance;
            }
        }

		protected bool isInitiated = false;
    }
}