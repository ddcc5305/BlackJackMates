using UnityEngine;
using UnityEngine.UI;

public class Deck : MonoBehaviour
{
    public Sprite[] faces;
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

    // Apuestas
    public int bank = 1000;
    public int currentBet = 0;
    public Text bankText;
    public Text betText;

    private void Start()
    {
        UpdateUI();
        ShuffleCards();
        hitButton.interactable = false; // Desactivar "Hit" al inicio
        stickButton.interactable = false; // Desactivar "Stand" al inicio
    }

    public void OnBetSelected()
    {
        string selectedBet = betDropdown.options[betDropdown.value].text;
        int betAmount = int.Parse(selectedBet);
        PlaceBet(betAmount);
    }

    public void PlaceBet(int amount)
    {
        if (amount <= bank && amount % 10 == 0)
        {
            currentBet = amount;
            bank -= amount;
            UpdateUI();
            StartGame(); // Iniciar el juego después de realizar la apuesta
        }
        else
        {
            finalMessage.text = "Apuesta no válida.";
        }
    }

    private void UpdateUI()
    {
        bankText.text = $"Dinero: {bank}";
        betText.text = $"Apuesta: {currentBet}";
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

    private void Awake()
    {
        InitCardValues();
    }

    private void InitCardValues()
    {
        for (int i = 0; i < 52; i++)
        {
            int value = (i % 13) + 1;
            if (value > 10) value = 10;
            values[i] = value;
        }
    }

    private void ShuffleCards()
    {
        for (int i = 0; i < values.Length; i++)
        {
            int randomIndex = Random.Range(i, values.Length);
            (values[i], values[randomIndex]) = (values[randomIndex], values[i]);
            (faces[i], faces[randomIndex]) = (faces[randomIndex], faces[i]);
        }
    }

    void StartGame()
    {
        if (currentBet == 0)
        {
            finalMessage.text = "Place a bet to start the game.";
            hitButton.interactable = false;
            stickButton.interactable = false;
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            PushPlayer();
            PushDealer();
        }

        // Mostrar la primera carta del dealer
        dealer.GetComponent<CardHand>().InitialToggle();

        // Activar los botones "Hit" y "Stand"
        hitButton.interactable = true;
        stickButton.interactable = true;

        CalculateProbabilities();

        if (player.GetComponent<CardHand>().points == 21 || dealer.GetComponent<CardHand>().points == 21)
        {
            finalMessage.text = "Blackjack!";
            hitButton.interactable = false;
            stickButton.interactable = false;
            if (player.GetComponent<CardHand>().points == 21)
            {
                WinBet();
            }
            else
            {
                LoseBet();
            }
        }
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

    private void CalculateProbabilities()
    {
        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;
        int remainingCards = 52 - cardIndex;

        if (remainingCards == 0)
        {
            probMessage.text = "No hay cartas restantes.";
            return;
        }

        int dealerWins = 0;
        int player17to21 = 0;
        int playerBust = 0;

        for (int i = cardIndex; i < 52; i++)
        {
            if (dealerPoints + values[i] > playerPoints && dealerPoints + values[i] <= 21)
                dealerWins++;

            if (playerPoints + values[i] >= 17 && playerPoints + values[i] <= 21)
                player17to21++;

            if (playerPoints + values[i] > 21)
                playerBust++;
        }

        float probDealerWins = (float)dealerWins / remainingCards;
        float probPlayer17to21 = (float)player17to21 / remainingCards;
        float probPlayerBust = (float)playerBust / remainingCards;

        probMessage.text = $"Dealer Wins: {probDealerWins:P}\nPlayer 17-21: {probPlayer17to21:P}\nPlayer Bust: {probPlayerBust:P}";
    }

    public void Hit()
    {
        PushPlayer();

        if (player.GetComponent<CardHand>().points > 21)
        {
            finalMessage.text = "¡Jugador pierde!";
            hitButton.interactable = false;
            stickButton.interactable = false;
            LoseBet();
        }
    }

    public void Stand()
    {
        while (dealer.GetComponent<CardHand>().points < 17)
        {
            PushDealer();
        }

        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;

        if (dealerPoints > 21 || playerPoints > dealerPoints)
        {
            finalMessage.text = "¡Jugador gana!";
            WinBet();
        }
        else if (playerPoints < dealerPoints)
        {
            finalMessage.text = "Dealer gana!";
            LoseBet();
        }
        else
        {
            finalMessage.text = "Empate!";
            bank += currentBet;
            currentBet = 0;
            UpdateUI();
        }

        hitButton.interactable = false;
        stickButton.interactable = false;
    }

    public void PlayAgain()
    {
        if (bank <= 0)
        {
            finalMessage.text = "¡Te quedaste sin créditos!";
            return;
        }

        hitButton.interactable = true;
        stickButton.interactable = true;
        finalMessage.text = "";
        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();
        cardIndex = 0;
        ShuffleCards();
        StartGame();
    }
}