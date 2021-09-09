﻿using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Utils;

namespace Client.Utils
{
    public class ClientConnectionStatus : ObservableObject
    {
        private string _textContent;

        public string TextContent
        {
            get { return _textContent; }
            set { _textContent = value; OnPropertyChanged(); }
        }

        private SolidColorBrush _textColor;

        public SolidColorBrush TextColor
        {
            get { return _textColor; }
            set { _textColor = value; OnPropertyChanged(); }
        }


        private SolidColorBrush _borderColor;

        public SolidColorBrush BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; OnPropertyChanged(); }
        }

        private double _borderThickness;

        public double BorderThickness
        {
            get { return _borderThickness; }
            set { _borderThickness = value; OnPropertyChanged(); }
        }


        private SolidColorBrush _backgroundColor;

        public SolidColorBrush BackgroundColor
        {
            get { return _backgroundColor; }
            set { _backgroundColor = value; OnPropertyChanged(); }
        }

        public ClientConnectionStatus(Color textColor, string textContent, Color borderColor, double borderThickness, Color backgroundColor)
        {
            TextColor = new SolidColorBrush(textColor);
            TextContent = textContent;
            BorderColor = new SolidColorBrush(borderColor);
            BorderThickness = borderThickness;
            BackgroundColor = new SolidColorBrush(backgroundColor);
        }

        public static ClientConnectionStatus DISCONNECTED = new(Colors.White, "DISCONNECTED", Color.FromRgb(251, 105, 98), 3, Color.FromRgb(208, 126, 126));
        public static ClientConnectionStatus CONNECTING = new(Colors.White, "CONNECTING", Color.FromRgb(251, 105, 98), 3, Color.FromRgb(208, 126, 126));
        public static ClientConnectionStatus CONNECTED = new(Colors.White, "CONNECTED", Color.FromRgb(251, 105, 98), 3, Color.FromRgb(208, 126, 126));
        public static ClientConnectionStatus PREGAME = new(Colors.White, "IN CHAMP SELECT", Color.FromRgb(251, 105, 98), 3, Color.FromRgb(208, 126, 126));
        public static ClientConnectionStatus INGAME = new(Colors.White, "INGAME", Color.FromRgb(251, 105, 98), 3, Color.FromRgb(208, 126, 126));
        public static ClientConnectionStatus POSTGAME = new(Colors.White, "IN POST GAME", Color.FromRgb(251, 105, 98), 3, Color.FromRgb(208, 126, 126));

        public static Dictionary<ConnectionStatus, ClientConnectionStatus> ConnectionStatusMap = new Dictionary<ConnectionStatus, ClientConnectionStatus>() {
            {ConnectionStatus.Disconnected, DISCONNECTED},
            {ConnectionStatus.Connected, CONNECTED},
            {ConnectionStatus.Connecting, CONNECTING},
            {ConnectionStatus.PreGame, PREGAME},
            {ConnectionStatus.Ingame, INGAME},
            {ConnectionStatus.PostGame, POSTGAME},
        };
    }
}
