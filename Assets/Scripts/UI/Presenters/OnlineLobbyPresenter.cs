using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.Core.Multiplayer;
using DemocracySim.Engine.Core.Multiplayer.Online;
using DemocracySim.Engine.World;

namespace DemocracySim.UI.Presenters
{
    /// <summary>
    /// Mirror tabanlı Çevrim İçi Lobi Arayüzü.
    /// Host kurma, IP ile bağlanma, ülke seçimi, oyuncu listesi ve sohbet alanı.
    /// </summary>
    public class OnlineLobbyPresenter
    {
        private readonly UIManager ui;
        private DemocracyNetworkManager net;
        private string targetIp = "127.0.0.1";
        private string chatInputText = "";
        private bool isReady = false;

        public OnlineLobbyPresenter(UIManager ui)
        {
            this.ui = ui;
        }

        public void Show()
        {
            net = DemocracyNetworkManager.EnsureInstance();

            ui.OpenModal(900, modal =>
            {
                RenderModal(modal);
            });
        }

        private void RenderModal(RectTransform modal)
        {
            HudKit.ClearChildren(modal);
            HudKit.VStack(modal.gameObject, 12, 14);

            // Başlık
            var header = HudKit.NewRect(modal, "Header");
            HudKit.HStack(header.gameObject, 10, 0, TextAnchor.MiddleLeft, false, true);
            HudKit.Label(header, "ÇEVRİM İÇİ ÇOK OYUNCULU (MIRROR NETWORK)", 28, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);

            var spacer = HudKit.NewRect(header, "Spacer");
            HudKit.Size(spacer.gameObject, flexW: 1);

            var closeBtn = HudKit.MakeButton(header, "X", HudTheme.PanelHi, HudTheme.Dim, 20, () =>
            {
                if (net != null && net.IsOnlineActive) net.DisconnectSession();
                ui.CloseModal();
            }, 36);
            HudKit.Size(closeBtn.gameObject, prefW: 42);

            HudKit.Label(modal, "Aynı yerel ağda (LAN) veya IP adresi üzerinden arkadaşlarınızla birlikte siyaset simülasyonu yapın.", 18, HudTheme.Dim);

            if (!net.IsOnlineActive)
            {
                RenderConnectionSetup(modal);
            }
            else
            {
                RenderActiveLobby(modal);
            }
        }

        private void RenderConnectionSetup(RectTransform parent)
        {
            var card = ui.Card(parent, "Bağlantı Kurulumu");
            HudKit.VStack(card.gameObject, 12, 12);

            // Oyuncu Adı
            HudKit.Label(card, "Oyuncu Adınız:", 19, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            CreateInputField(card, PlayerProfile.PlayerName, 24, val =>
            {
                if (!string.IsNullOrWhiteSpace(val)) PlayerProfile.PlayerName = val.Trim();
            });

            // 1) Sunucu Kur (Host) Butonu
            var hostRow = HudKit.NewRect(card, "HostRow");
            HudKit.HStack(hostRow.gameObject, 10, 0, TextAnchor.MiddleLeft, false, true);
            var hostBtn = HudKit.MakeButton(hostRow, "[SUNUCU KUR (HOST)]", HudTheme.Gold, HudTheme.Dark, 22, () =>
            {
                net.HostGame(PlayerProfile.PlayerName, "turkey");
                ui.Notify("Sunucu kuruldu! Oyuncular bekleniyor...", false);
                ui.CloseModal();
                Show();
            }, 54);
            HudKit.Size(hostBtn.gameObject, flexW: 1);

            HudKit.Label(card, "Veya var olan bir sunucuya bağlanın:", 19, HudTheme.Text, TextAlignmentOptions.Left);

            // 2) İstemci Olarak Bağlan
            var joinRow = HudKit.NewRect(card, "JoinRow");
            HudKit.HStack(joinRow.gameObject, 10, 0, TextAnchor.MiddleLeft, false, true);

            var ipInput = CreateInputField(joinRow, targetIp, 20, val => targetIp = val);
            HudKit.Size(ipInput.gameObject, prefW: 300);

            var joinBtn = HudKit.MakeButton(joinRow, "[BAĞLAN (JOIN)]", HudTheme.Action, Color.white, 22, () =>
            {
                net.JoinGame(targetIp, PlayerProfile.PlayerName, "usa");
                ui.Notify($"Sunucuya bağlanılıyor: {targetIp}", false);
                ui.CloseModal();
                Show();
            }, 54);
            HudKit.Size(joinBtn.gameObject, flexW: 1);
        }

        private void RenderActiveLobby(RectTransform parent)
        {
            // Durum Rozeti
            string statusStr = net.IsServerHost ? "[ROL: SUNUCU / HOST]" : "[ROL: İSTEMCİ / CLIENT]";
            Color statusCol = net.IsServerHost ? HudTheme.Gold : HudTheme.Good;
            HudKit.Label(parent, $"{statusStr}  •  Bağlı Oyuncular: {net.LobbyPlayers.Count}", 22, statusCol, TextAlignmentOptions.Left, FontStyles.Bold);

            // Oyuncular Listesi
            var listCard = ui.Card(parent, "Lobideki Oyuncular");
            HudKit.VStack(listCard.gameObject, 8, 8);

            foreach (var p in net.LobbyPlayers)
            {
                var row = HudKit.NewRect(listCard, "PlayerRow");
                HudKit.HStack(row.gameObject, 10, 0, TextAnchor.MiddleLeft, false, true);

                string roleTag = p.IsHost ? "<color=#F2B84B>[HOST]</color> " : "";
                HudKit.Label(row, $"{roleTag}<b>{UIManager.Clean(p.PlayerName)}</b>", 20, HudTheme.Text);

                var spacer = HudKit.NewRect(row, "Spacer");
                HudKit.Size(spacer.gameObject, flexW: 1);

                HudKit.Label(row, $"Ülke: {p.CountryId.ToUpper()}", 18, HudTheme.Dim);

                Color readyCol = p.IsReady ? HudTheme.Good : HudTheme.Warn;
                string readyTxt = p.IsReady ? "[HAZIR]" : "[BEKLİYOR]";
                HudKit.Label(row, readyTxt, 18, readyCol, TextAlignmentOptions.Right, FontStyles.Bold);
            }

            // Eylem Butonları
            var actionRow = HudKit.NewRect(parent, "ActionRow");
            HudKit.HStack(actionRow.gameObject, 10, 0, TextAnchor.MiddleLeft, false, true);

            // Hazır Ol Toggle
            var readyBtn = HudKit.MakeButton(actionRow, isReady ? "[HAZIR DEĞİLİM]" : "[HAZIRIM]",
                isReady ? HudTheme.Warn : HudTheme.Good, HudTheme.Dark, 20, () =>
                {
                    isReady = !isReady;
                    net.SendReadyToggle(isReady);
                    ui.CloseModal();
                    Show();
                }, 50);
            HudKit.Size(readyBtn.gameObject, prefW: 200);

            // Host için: Oyunu Başlat
            if (net.IsServerHost)
            {
                bool canStart = net.LobbyPlayers.Count >= 1 && net.LobbyPlayers.All(p => p.IsReady);
                var startBtn = HudKit.MakeButton(actionRow, "[ÇEVRİM İÇİ OYUNU BAŞLAT]",
                    canStart ? HudTheme.Gold : HudTheme.Dim, HudTheme.Dark, 20, () =>
                    {
                        if (canStart)
                        {
                            net.ServerStartGame();
                            ui.CloseModal();
                            ui.Notify("Çevrim içi oyun başlatıldı!", false);
                        }
                        else
                        {
                            ui.Notify("Tüm oyuncuların hazır olması gerekiyor!", true);
                        }
                    }, 50);
                HudKit.Size(startBtn.gameObject, flexW: 1);
            }

            // Bağlantıyı Kes Butonu
            var discBtn = HudKit.MakeButton(actionRow, "[AYRIL]", HudTheme.Bad, Color.white, 20, () =>
            {
                net.DisconnectSession();
                ui.CloseModal();
                Show();
            }, 50);
            HudKit.Size(discBtn.gameObject, prefW: 140);

            // Sohbet Alanı
            var chatCard = ui.Card(parent, "Diplomatik İletişim / Sohbet");
            HudKit.VStack(chatCard.gameObject, 6, 8);

            var chatScroll = HudKit.ScrollView(chatCard, "ChatLog", out var chatContent, 6, 6);
            HudKit.Size(chatScroll.gameObject, prefH: 120, minH: 120);
            foreach (var msg in net.ChatMessages.TakeLast(8))
            {
                HudKit.Label(chatContent, "• " + UIManager.Clean(msg), 16, HudTheme.Text);
            }

            var sendRow = HudKit.NewRect(chatCard, "SendRow");
            HudKit.HStack(sendRow.gameObject, 8, 0, TextAnchor.MiddleLeft, false, true);
            var chatField = CreateInputField(sendRow, chatInputText, 18, val => chatInputText = val);
            HudKit.Size(chatField.gameObject, flexW: 1);

            var sendBtn = HudKit.MakeButton(sendRow, "Gönder", HudTheme.Action, Color.white, 18, () =>
            {
                if (!string.IsNullOrWhiteSpace(chatInputText))
                {
                    net.SendChat(chatInputText);
                    chatInputText = "";
                    ui.CloseModal();
                    Show();
                }
            }, 42);
            HudKit.Size(sendBtn.gameObject, prefW: 120);
        }

        private RectTransform CreateInputField(Transform parent, string initialText, float fontSize, Action<string> onValueChange)
        {
            var go = HudKit.NewRect(parent, "InputBox");
            HudKit.Size(go.gameObject, prefH: 48, minH: 48);

            var bg = HudKit.Box(go, "Bg", HudTheme.PanelHi);
            HudKit.Fill(bg.rectTransform);

            var input = go.gameObject.AddComponent<TMP_InputField>();
            var textArea = HudKit.NewRect(go, "TextArea");
            HudKit.Fill(textArea, 10, 6, 10, 6);

            var placeholder = HudKit.Label(textArea, "...", fontSize, HudTheme.Dim, TextAlignmentOptions.Left);
            var text = HudKit.Label(textArea, initialText, fontSize, HudTheme.Text, TextAlignmentOptions.Left);

            input.textViewport = textArea;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = initialText;

            input.onValueChanged.AddListener(v => onValueChange?.Invoke(v));
            return go;
        }
    }
}
