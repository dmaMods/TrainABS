using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace dmaTrainABS
{
    public class OptionsKeymapping : UICustomControl
    {
        private static readonly string kKeyBindingTemplate = "KeyBindingTemplate";
        private SavedInputKey m_EditingBinding;
        private string m_EditingBindingCategory;
        private int count;

        private void AddKeymapping(string label, SavedInputKey savedInputKey)
        {
            UIPanel panel = base.component.AttachUIComponent(UITemplateManager.GetAsGameObject(kKeyBindingTemplate)) as UIPanel;
            int count = this.count;
            this.count = count + 1;
            if ((count % 2) == 1)
            {
                panel.backgroundSprite = null;
            }
            UILabel label2 = panel.Find<UILabel>("Name");
            UIButton local1 = panel.Find<UIButton>("Binding");
            local1.eventKeyDown += new KeyPressHandler(this.OnBindingKeyDown);
            local1.eventMouseDown += new MouseEventHandler(this.OnBindingMouseDown);
            label2.text = label;
            local1.text = savedInputKey.ToLocalizedString("KEYNAME");
            local1.objectUserData = savedInputKey;
        }

        private void Awake()
        {
            this.AddKeymapping("Mod shortcut", TrainABSModData.ModShortcut);
            this.AddKeymapping("Update network", TrainABSModData.NetReload);
            this.AddKeymapping("Green lights to all", TrainABSModData.AllGreenLights);
            this.AddKeymapping("Red lights to all", TrainABSModData.AllRedLights);
            this.AddKeymapping("Show Blocks", TrainABSModData.ShowBlocks);
        }

        private KeyCode ButtonToKeycode(UIMouseButton button) =>
            (button != UIMouseButton.Left) ? ((button != UIMouseButton.Right) ? ((button != UIMouseButton.Middle) ? ((button != UIMouseButton.Special0) ? ((button != UIMouseButton.Special1) ? ((button != UIMouseButton.Special2) ? ((button != UIMouseButton.Special3) ? KeyCode.None : KeyCode.Mouse6) : KeyCode.Mouse5) : KeyCode.Mouse4) : KeyCode.Mouse3) : KeyCode.Mouse2) : KeyCode.Mouse1) : KeyCode.Mouse0;

        //internal InputKey GetDefaultEntry(string entryName)
        //{
        //    FieldInfo field = typeof(DefaultSettings).GetField(entryName, BindingFlags.Public | BindingFlags.Static);
        //    if (field == null)
        //    {
        //        return 0;
        //    }
        //    object obj2 = field.GetValue(null);
        //    return (!(obj2 is InputKey) ? 0 : ((InputKey)obj2));
        //}

        private bool IsAltDown() =>
            Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        private bool IsControlDown() =>
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        private bool IsModifierKey(KeyCode code) =>
            (code == KeyCode.LeftControl) || ((code == KeyCode.RightControl) || ((code == KeyCode.LeftShift) || ((code == KeyCode.RightShift) || ((code == KeyCode.LeftAlt) || (code == KeyCode.RightAlt)))));

        private bool IsShiftDown() =>
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        private bool IsUnbindableMouseButton(UIMouseButton code) =>
            (code == UIMouseButton.Left) || (code == UIMouseButton.Right);

        private void OnBindingKeyDown(UIComponent comp, UIKeyEventParameter p)
        {
            if ((this.m_EditingBinding != null) && !this.IsModifierKey(p.keycode))
            {
                p.Use();
                UIView.PopModal();
                KeyCode keycode = p.keycode;
                InputKey empty = (p.keycode == KeyCode.Escape) ? this.m_EditingBinding.value : SavedInputKey.Encode(keycode, p.control, p.shift, p.alt);
                if (p.keycode == KeyCode.Backspace)
                {
                    empty = SavedInputKey.Empty;
                }
                this.m_EditingBinding.value = empty;
                (p.source as UITextComponent).text = this.m_EditingBinding.ToLocalizedString("KEYNAME");
                this.m_EditingBinding = null;
                this.m_EditingBindingCategory = string.Empty;
            }
        }

        private void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p)
        {
            if (this.m_EditingBinding == null)
            {
                p.Use();
                this.m_EditingBinding = (SavedInputKey)p.source.objectUserData;
                this.m_EditingBindingCategory = p.source.stringUserData;
                UIButton source = p.source as UIButton;
                source.buttonsMask = UIMouseButton.Special3 | UIMouseButton.Special2 | UIMouseButton.Special1 | UIMouseButton.Special0 | UIMouseButton.Middle | UIMouseButton.Right | UIMouseButton.Left;
                source.text = "Press any key";
                p.source.Focus();
                UIView.PushModal(p.source);
            }
            else if (!this.IsUnbindableMouseButton(p.buttons))
            {
                p.Use();
                UIView.PopModal();
                InputKey key = SavedInputKey.Encode(this.ButtonToKeycode(p.buttons), this.IsControlDown(), this.IsShiftDown(), this.IsAltDown());
                this.m_EditingBinding.value = key;
                UIButton source = p.source as UIButton;
                source.text = this.m_EditingBinding.ToLocalizedString("KEYNAME");
                source.buttonsMask = UIMouseButton.Left;
                this.m_EditingBinding = null;
                this.m_EditingBindingCategory = string.Empty;
            }
        }

        private void OnDisable() { }

        private void OnEnable() { }

        private void OnLocaleChanged()
        {
            this.RefreshBindableInputs();
        }

        private void RefreshBindableInputs()
        {
            UIComponent[] componentsInChildren = base.component.GetComponentsInChildren<UIComponent>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                UIComponent component1 = componentsInChildren[i];
                UITextComponent component = component1.Find<UITextComponent>("Binding");
                if (component != null)
                {
                    SavedInputKey objectUserData = component.objectUserData as SavedInputKey;
                    if (objectUserData != null)
                    {
                        component.text = objectUserData.ToLocalizedString("KEYNAME");
                    }
                }
                UILabel label = component1.Find<UILabel>("Name");
                if (label != null)
                {
                    label.text = label.stringUserData;
                }
            }
        }

        private void RefreshKeyMapping()
        {
            UIComponent[] componentsInChildren = base.component.GetComponentsInChildren<UIComponent>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                UITextComponent component = componentsInChildren[i].Find<UITextComponent>("Binding");
                SavedInputKey objectUserData = (SavedInputKey)component.objectUserData;
                if (this.m_EditingBinding != objectUserData)
                {
                    component.text = objectUserData.ToLocalizedString("KEYNAME");
                }
            }
        }

    }
}

