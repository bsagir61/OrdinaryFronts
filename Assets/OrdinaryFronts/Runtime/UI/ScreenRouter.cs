using System.Collections.Generic;
using UnityEngine;

namespace OrdinaryFronts
{
    public enum AppScreen
    {
        None,
        MainMenu,
        StorySelect,
        Intro,
        Gameplay,
        Settings,
        Pause,
        Journal,
        RouteMap,
        Interlude,
        Testimony,
        Ending,
        Error
    }

    public sealed class ScreenRouter
    {
        private readonly Dictionary<AppScreen, GameObject> screens = new Dictionary<AppScreen, GameObject>();
        public AppScreen Current { get; private set; }

        public void Register(AppScreen id, GameObject screen)
        {
            screens[id] = screen;
            screen.SetActive(false);
        }

        public void Show(AppScreen id)
        {
            foreach (KeyValuePair<AppScreen, GameObject> pair in screens)
                pair.Value.SetActive(pair.Key == id);
            Current = id;
        }

        public GameObject Get(AppScreen id)
        {
            GameObject result;
            return screens.TryGetValue(id, out result) ? result : null;
        }
    }
}
