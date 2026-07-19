# Script Teardown & Deterministic Cleanup Protocol

During Hot Reloading, a collectible `AssemblyLoadContext` is created to load the compiled game assembly. If any references to instances of types defined in the dynamic assembly leak into the main IDE assembly, the garbage collector will not be able to unload the context. This manual documents the deterministic script teardown protocol designed to prevent memory leaks.

---

## 1. High-Level Description
The script teardown protocol ensures that all dynamic script behaviors attached to active entities are disposed of, event subscriptions (e.g. from static classes or event managers) are cleaned up, and all local and reflection-cached structures are cleared before unloading the old assembly.

---

## 2. API Signatures

### `EntityBehavior` and `IEntityScript` Teardown Contract
All runtime scripts inherit from `EntityBehavior` (or legacy `IEntityScript`), which now implements `System.IDisposable`.

```csharp
namespace MonoGameMaker.Runtime
{
    public interface IDisposable
    {
        void Dispose();
    }

    public abstract class EntityBehavior : IDisposable
    {
        // ... base properties ...
        public virtual void Dispose();
    }
}
```

### `EntityManager.PurgeAllScripts` API
Forces disposal of all active and pending scripts and clears all internal collections and reflection caches inside `EntityManager`.

```csharp
namespace MonoGameMaker.Runtime
{
    public static class EntityManager
    {
        public static void PurgeAllScripts();
    }
}
```

---

## 3. Use Cases (Context for AIs)
- **Global Event Unsubscription**: If a script registers to an event on a static class (e.g., a global `InputManager`, `SoundManager`, or custom message bus), that registration creates a strong reference from the static event to the script. To allow the script assembly to be unloaded, the script must override `Dispose()` and unsubscribe from the event.
- **Cache Purging**: The `EntityManager` maintains caches and active entity lists. `PurgeAllScripts()` clears these lists and uses reflection to null/clear any static Type, MethodInfo, dictionary, or list fields to prevent retention of old assembly references.

---

## 4. Practical & Compilable Example

Below is a compilable script behavior demonstrating a custom event listener that cleans up its event handler inside `Dispose()`, preventing memory leaks:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    // A mock global event publisher that could cause leaks if not unsubscribed
    public static class GlobalGameEvents
    {
        public static event Action<string>? OnGameNotification;

        public static void TriggerNotification(string message)
        {
            OnGameNotification?.Invoke(message);
        }
    }

    public class PlayerNotificationLogger : EntityBehavior
    {
        private List<string> _notifications = new List<string>();

        public override void Awake()
        {
            // Subscribe to a global static event
            GlobalGameEvents.OnGameNotification += HandleNotification;
        }

        private void HandleNotification(string message)
        {
            _notifications.Add(message);
            if (_notifications.Count > 5)
            {
                _notifications.RemoveAt(0);
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Update logic here
        }

        public override void Dispose()
        {
            // MANDATORY: Unsubscribe to release the reference and allow Assembly unloading
            GlobalGameEvents.OnGameNotification -= HandleNotification;
            _notifications.Clear();
        }
    }
}
```
