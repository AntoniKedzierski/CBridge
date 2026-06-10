namespace Model.Bidding.AI; 

public enum BiddingGoal {
    Undefined,          // Jeszcze nie określony (tylko w pierwszym kółku)
    Pass,               // Plaża
    SystemDefence,      // Obrona wg schematów (kontra wywoławcza, wejście kolorem)
    Game,               // Szukamy partii
    GameForcing,        // Sforsowani do partii
    PremiumContract,    // Szlem lub szlemik
    MinimizeLoss,       // Minimalizacja straty przez wpadkę
    PlayForPenalty,     // Granie pod wpadkę przeciwników (kontra)
}