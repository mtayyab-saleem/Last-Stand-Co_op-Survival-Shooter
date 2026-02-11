using UnityEngine;
using UnityEngine.UI;
using JUTPS.InputEvents;
using UnityEngine.Events;

namespace JUTPS.UI
{
    /// <summary>
    /// Creates an UI tab system, allowing the user navigate between "pages" in an UI.
    /// </summary>
    public class JU_UITabs : MonoBehaviour
    {
        /// <summary>
        /// The tab navigation mode, used when the user tries go to another tab, specially if is the first or lasted tab.
        /// </summary>
        public enum NavigationModes
        {
            /// <summary>
            /// The user can't go to the next tab if the current thab is the lasted or the firs.
            /// </summary>
            StopOnEnd,

            /// <summary>
            /// Restarts the navigation moving to the the first/last tab when try access other tag out of the tabs limits.
            /// </summary>
            Restart
        }

        /// <summary>
        /// Stores a Tab settings.
        /// </summary>
        [System.Serializable]
        public class Tab
        {
            private bool _isSelected;

            /// <summary>
            /// The button to select this tab on UI.
            /// </summary>
            public Button Button;

            /// <summary>
            /// The image that be actived when this tab is selected, used only to provide a better
            /// UI style on click on the <see cref="Tab.Button"/> on when <see cref="IsSelected"/> is true.
            /// </summary>
            public GameObject SelectedImage;

            /// <summary>
            /// The image that be actived when this tab isn't selected.
            /// </summary>
            public GameObject NoSelectedImage;

            /// <summary>
            /// The tab screen, actived then <see cref="IsSelected"/> is true or when the user click on the <see cref="Tab.Button"/>
            /// </summary>
            public GameObject TabScreen;

            /// <summary>
            /// Returns true if this tab is selected.
            /// </summary>
            public bool IsSelected
            {
                get => _isSelected;
                internal set
                {
                    if (value == _isSelected)
                        return;

                    _isSelected = value;
                    OnSetSelect(value);
                }
            }

            internal void Refresh()
            {
                OnSetSelect(IsSelected);
            }

            private void OnSetSelect(bool selected)
            {
                if (SelectedImage) SelectedImage.SetActive(selected);
                if (NoSelectedImage) NoSelectedImage.SetActive(!selected);
                if (TabScreen) TabScreen.SetActive(selected);
            }
        }

        /// <summary>
        /// Used to navigate to the next tab when press some button.
        /// </summary>
        public MultipleActionEvent NextTabAction;

        /// <summary>
        /// Used to navigate to the previous tab when press some button.
        /// </summary>
        public MultipleActionEvent PreviousTabAction;

        /// <summary>
        /// The tabs navigation mode.
        /// </summary>
        public NavigationModes NavigationMode;

        /// <summary>
        /// All tabs.
        /// </summary>
        public Tab[] Tabs;

        /// <summary>
        /// An event that can be called when the user changes the current tab selected, see also <seealso cref="CurrentTabIndex"/>.
        /// </summary>
        public UnityEvent<Tab> OnChangeTab;

        /// <summary>
        /// The current index of the tab selected from <see cref="Tabs"/>.
        /// </summary>
        public int CurrentTabIndex { get; private set; }

        private void Awake()
        {
            SetupTabs();
        }

        private void OnEnable()
        {
            NextTabAction.Enable();
            PreviousTabAction.Enable();
        }

        private void OnDisable()
        {
            NextTabAction.Disable();
            PreviousTabAction.Disable();
        }

        private void SetupTabs()
        {
            for (int i = 0; i < Tabs.Length; i++)
            {
                if (Tabs[i].Button)
                {
                    int tabIndex = i;
                    Tabs[i].Button.onClick.AddListener(() => SelectTab(tabIndex));
                }
            }

            for (int i = 0; i < Tabs.Length; i++)
            {
                Tabs[i].IsSelected = i == 0;
                Tabs[i].Refresh();
            }

            NextTabAction.OnButtonsDown.AddListener(OnPressButtonNextTab);
            PreviousTabAction.OnButtonsDown.AddListener(OnPressButtonPreviousTab);
        }

        private void OnPressButtonNextTab()
        {
            SetNextTab();
        }

        private void OnPressButtonPreviousTab()
        {
            SetPreviousTab();
        }

        private int FindNextTabIndex(int index)
        {
            if (NavigationMode == NavigationModes.Restart)
            {
                if (index > Tabs.Length - 1)
                    return 0;

                if (index < 0)
                    return Mathf.Max(Tabs.Length - 1, 0);
            }

            if (NavigationMode == NavigationModes.StopOnEnd)
                return Mathf.Clamp(index, 0, Tabs.Length - 1);

            return index;
        }

        /// <summary>
        /// Select a <see cref="Tab"/> from <see cref="Tabs"/> using an index.
        /// </summary>
        /// <param name="index"></param>
        public void SelectTab(int index)
        {
            if (index < 0 || index > Tabs.Length - 1)
            {
                Debug.LogError($"Can't select tab with index {index} on {name} gameObject.");
                return;
            }

            CurrentTabIndex = index;

            for (int i = 0; i < Tabs.Length; i++)
                Tabs[i].IsSelected = index == i;

            OnChangeTab.Invoke(Tabs[index]);
        }

        /// <summary>
        /// Navigate to the next tab.
        /// </summary>
        public void SetNextTab()
        {
            int index = FindNextTabIndex(CurrentTabIndex + 1);
            SelectTab(index);
        }

        /// <summary>
        /// Navigate to the previous tab.
        /// </summary>
        public void SetPreviousTab()
        {
            int index = FindNextTabIndex(CurrentTabIndex - 1);
            SelectTab(index);
        }

        /// <summary>
        /// Call it to update the current active tab and desactive all unselected tabs. 
        /// </summary>
        public void Refresh()
        {
            for (int i = 0; i < Tabs.Length; i++)
                Tabs[i].Refresh();
        }
    }
}