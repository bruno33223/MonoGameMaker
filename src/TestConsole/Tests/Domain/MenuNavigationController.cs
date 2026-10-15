using System;

namespace TestConsole.Tests.Domain
{
    /// <summary>
    /// Controlador de navegação cardinal para menus com debounce de repetição (0.4s initial delay).
    /// </summary>
    public class MenuNavigationController
    {
        public const float InitialRepeatDelay = 0.40f;
        public const float RepeatInterval = 0.10f;

        private float _holdTimer = 0f;
        private bool _isHeld = false;

        public int CurrentIndex { get; set; } = 0;
        public int MaxItems { get; set; } = 5;

        public bool UpdateDirectionHold(bool isDownPressed, float dt)
        {
            if (isDownPressed)
            {
                if (!_isHeld)
                {
                    // Primeiro toque: salto instantâneo
                    _isHeld = true;
                    _holdTimer = 0f;
                    CurrentIndex = (CurrentIndex + 1) % MaxItems;
                    return true;
                }
                else
                {
                    _holdTimer += dt;
                    // Aguarda 0.4s antes de acionar repetição
                    if (_holdTimer >= InitialRepeatDelay)
                    {
                        _holdTimer -= RepeatInterval;
                        CurrentIndex = (CurrentIndex + 1) % MaxItems;
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                _isHeld = false;
                _holdTimer = 0f;
                return false;
            }
        }
    }
}
