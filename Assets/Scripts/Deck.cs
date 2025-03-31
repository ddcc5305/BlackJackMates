using System;
using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public class Deck : MonoBehaviour
{
    public Sprite[] faces;
    public Sprite cardBack;
    public GameObject dealer;
    public GameObject player;
    public Button hitButton;
    public Button stickButton;
    public Button playAgainButton;
    public Text finalMessage;
    public Text probMessage;
    public Dropdown betDropdown;

    public int[] values = new int[52];
    int cardIndex = 0;

    // Sistema de apuestas
    public int bank = 1000;
    public int currentBet = 0;
    public Text bankText;
    public Text betText;

    private void Start()
    {
        InitCardValues();
        ShuffleCards();
        UpdateUI();
        SetupBetDropdown();
        DisableGameControls();
    }

    private void SetupBetDropdown()
    {
        betDropdown.ClearOptions();

        betDropdown.options.Add(new Dropdown.OptionData("Selecciona apuesta"));

        int[] betMultiples = { 10, 100, 1000 };
        foreach (int multiple in betMultiples)
        {
            if (multiple <= bank)
            {
                betDropdown.options.Add(new Dropdown.OptionData(multiple.ToString()));
            }
        }

        betDropdown.RefreshShownValue();
        betDropdown.onValueChanged.AddListener(OnBetSelected);
    }

    private void InitCardValues()
    {
        for (int i = 0; i < 52; i++)
        {
            int value = i % 13 + 1;
            values[i] = Mathf.Min(value, 10); // Asigna 10 a J, Q, K
            if (value == 1) values[i] = 11;   // Asigna 11 al As
        }
    }

    private void ShuffleCards()
    {
        System.Random rng = new System.Random();

        for (int i = 0; i < values.Length; i++)
        {
            int randomIndex = rng.Next(i, values.Length);

            // Swap values
            int tempValue = values[i];
            values[i] = values[randomIndex];
            values[randomIndex] = tempValue;

            // Swap sprites
            Sprite tempSprite = faces[i];
            faces[i] = faces[randomIndex];
            faces[randomIndex] = tempSprite;
        }
        cardIndex = 0;
    }

    public void OnBetSelected(int index)
    {
        if (index == 0 || index >= betDropdown.options.Count)
        {
            betDropdown.value = 0;
        }

        string selectedOption = betDropdown.options[index].text;
        if (int.TryParse(selectedOption, out int betAmount))
        {
            // Solo procesar si no hay apuesta actual
            if (currentBet == 0)
            {
                PlaceBet(betAmount);
                // Resetear el dropdown después de seleccionar
                StartCoroutine(ResetDropdownAfterDelay());
            }
        }
    }

    private IEnumerator ResetDropdownAfterDelay()
    {
        yield return new WaitForEndOfFrame(); // Esperar un frame
        betDropdown.value = 0; // Volver al placeholder
    }

    public void PlaceBet(int amount)
    {
        if (amount <= bank && amount > 0)
        {
            currentBet = amount;
            bank -= amount;
            UpdateUI();
            betDropdown.interactable = false;
            StartGame();
        }
    }

    public void StartGame()
    {
        if (currentBet == 0) return;

        // Limpiar manos anteriores
        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();

        // Repartir cartas
        for (int i = 0; i < 2; i++)
        {
            PushPlayer();
            PushDealer();
        }

        // Ocultar primera carta del dealer
        dealer.GetComponent<CardHand>().cards[0].GetComponent<CardModel>().ToggleFace(false);

        // Verificar Blackjack inmediato
        CheckInitialBlackjack();

        // Solo calcular probabilidades si el juego continúa
        if (hitButton.interactable)
        {
            CalculateProbabilities();
        }
    }

    void CheckInitialBlackjack()
    {
        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;
        bool playerHasBlackjack = (playerPoints == 21 && player.GetComponent<CardHand>().cards.Count == 2);
        bool dealerHasBlackjack = (dealerPoints == 21 && dealer.GetComponent<CardHand>().cards.Count == 2);

        // Mostrar todas las cartas del dealer si alguien tiene blackjack
        if (playerHasBlackjack || dealerHasBlackjack)
        {
            dealer.GetComponent<CardHand>().cards[0].GetComponent<CardModel>().ToggleFace(true);
        }

        // Ambos tienen blackjack - empate
        if (playerHasBlackjack && dealerHasBlackjack)
        {
            finalMessage.text = "¡Ambos tienen Blackjack! Empate.";
            bank += currentBet; // Recupera la apuesta
            currentBet = 0;
            DisableGameControls();
            return;
        }

        // Solo el jugador tiene blackjack - paga 3:2
        if (playerHasBlackjack)
        {
            finalMessage.text = "¡Blackjack! Ganaste 1.5x la apuesta.";
            bank += (int)(currentBet * 2.5f); // 1.5x además de recuperar la apuesta
            currentBet = 0;
            DisableGameControls();
            return;
        }

        // Solo el dealer tiene blackjack - jugador pierde
        if (dealerHasBlackjack)
        {
            finalMessage.text = "Dealer tiene Blackjack! Pierdes.";
            currentBet = 0;
            DisableGameControls();
            return;
        }

        // Activar controles si no hay blackjacks
        hitButton.interactable = true;
        stickButton.interactable = true;
        finalMessage.text = "";
    }

    void PushDealer()
    {
        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
    }

    void PushPlayer()
    {
        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        CalculateProbabilities();
    }

    public void Hit()
    {
        PushPlayer();

        int playerPoints = player.GetComponent<CardHand>().points;

        if (playerPoints > 21)
        {
            finalMessage.text = "¡Te has pasado! Dealer gana.";
            LoseBet();
            EndGame();
        }
        else if (playerPoints == 21)
        {
            finalMessage.text = "¡21 puntos!";
            Stand(); // Automáticamente termina el turno
        }
    }

    public void Stand()
    {
        // Mostrar carta oculta del dealer
        dealer.GetComponent<CardHand>().cards[0].GetComponent<CardModel>().ToggleFace(true);

        // Repartir cartas al dealer según reglas
        while (dealer.GetComponent<CardHand>().points < 17)
        {
            PushDealer();
        }

        EndGame();
    }

    void EndGame()
    {
        DisableGameControls();

        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;

        if (playerPoints > 21)
        {
            finalMessage.text = "Dealer gana. Te pasaste de 21.";
            LoseBet();
        }
        else if (dealerPoints > 21 || playerPoints > dealerPoints)
        {
            finalMessage.text = "¡Ganaste!";
            WinBet();
        }
        else if (playerPoints < dealerPoints)
        {
            finalMessage.text = "Dealer gana.";
            LoseBet();
        }
        else
        {
            finalMessage.text = "Empate. Recuperas tu apuesta.";
            bank += currentBet;
            currentBet = 0;
            UpdateUI();
        }
    }

    private void CalculateProbabilities()
    {
        // Verificar que el dealer tenga al menos 2 cartas
        if (dealer.GetComponent<CardHand>().cards.Count < 2 || player.GetComponent<CardHand>().cards.Count < 2)
        {
            probMessage.text = "";
            return;
        }

        int playerPoints = player.GetComponent<CardHand>().points;

        // Calcular todas las probabilidades
        float dealerWinProb = CalculateProbabilityDealerWins(playerPoints);
        float playerGoodProb = CalculateProbabilityPlayer17to21(playerPoints);
        float playerBustProb = CalculateProbabilityPlayerBust(playerPoints);

        // Mostrar con 2 decimales
        probMessage.text = $"Gana dealer: {dealerWinProb:P2}\n" +
                         $"Jugador 17-21: {playerGoodProb:P2}\n" +
                         $"Jugador pierde: {playerBustProb:P2}";
    }

    private float CalculateProbabilityDealerWins(int playerPoints)
    {
        int dealerVisibleValue = dealer.GetComponent<CardHand>().cards[1].GetComponent<CardModel>().value;
        int dealerHiddenValue = dealer.GetComponent<CardHand>().cards[0].GetComponent<CardModel>().value;
        int remainingCards = 52 - cardIndex;

        if (remainingCards == 0) return 0f;

        int favorableCases = 0;
        int totalCases = 0;

        // Considerar la carta oculta del dealer
        int dealerTotal = dealerVisibleValue + dealerHiddenValue;
        if (dealerTotal > playerPoints && dealerTotal <= 21)
        {
            favorableCases++;
        }
        totalCases++;

        // Simular cada carta restante
        for (int i = cardIndex; i < values.Length; i++)
        {
            int newDealerTotal = dealerVisibleValue + values[i];

            // El dealer gana si:
            // 1. No se pasa de 21 Y
            // 2. Tiene más puntos que el jugador
            if (newDealerTotal > playerPoints && newDealerTotal <= 21)
            {
                favorableCases++;
            }
            totalCases++;
        }

        return (float)favorableCases / totalCases;
    }

    private float CalculateProbabilityPlayer17to21(int playerPoints)
    {
        int remainingCards = 52 - cardIndex;
        if (remainingCards == 0) return 0f;

        int favorableCases = 0;

        for (int i = cardIndex; i < values.Length; i++)
        {
            int newTotal = playerPoints + values[i];
            if (newTotal >= 17 && newTotal <= 21)
            {
                favorableCases++;
            }
        }

        return (float)favorableCases / remainingCards;
    }

    private float CalculateProbabilityPlayerBust(int playerPoints)
    {
        int remainingCards = 52 - cardIndex;
        if (remainingCards == 0) return 0f;

        int favorableCases = 0;

        for (int i = cardIndex; i < values.Length; i++)
        {
            if (playerPoints + values[i] > 21)
            {
                favorableCases++;
            }
        }

        return (float)favorableCases / remainingCards;
    }

    void DisableGameControls()
    {
        hitButton.interactable = false;
        stickButton.interactable = false;
    }

    public void WinBet()
    {
        bank += currentBet * 2;
        currentBet = 0;
        UpdateUI();
    }

    public void LoseBet()
    {
        currentBet = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        bankText.text = $"Bank: {bank}";
        betText.text = $"Bet: {currentBet}";
    }

    public void PlayAgain()
    {
        if (bank <= 0)
        {
            finalMessage.text = "¡Sin fondos!";
            return;
        }

        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();
        cardIndex = 0;
        ShuffleCards();
        currentBet = 0;
        UpdateUI();
        finalMessage.text = "Selecciona apuesta";
        probMessage.text = "";
        betDropdown.interactable = true;
        betDropdown.value = 0;
        SetupBetDropdown();
    }
}