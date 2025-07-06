using System;
using UnityEngine;

public interface IHasProgess {
    public event EventHandler<OnProgressChangedEventArgs> OnProgressChanged;
    public class OnProgressChangedEventArgs : EventArgs {
        public float ProgressNormalized;
    }
}
