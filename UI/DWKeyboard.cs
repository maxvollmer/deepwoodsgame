﻿using DeepWoods.Game;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace DeepWoods.UI
{
    internal class DWKeyboard
    {
        public static KeyboardState GetState(PlayerIndex playerIndex)
        {
            if (OperatingSystem.IsWindows() && GameState.IsMultiplayerGame)
            {
                return RawInput.GetKeyboardState(playerIndex);
            }
            else if (playerIndex == PlayerIndex.One)
            {
                return Keyboard.GetState();
            }
            else
            {
                return default;
            }
        }
    }
}
