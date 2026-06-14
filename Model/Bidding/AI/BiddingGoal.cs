namespace Model.Bidding.AI; 

public enum BiddingGoal {
    None,           // Jeszcze nie określony (tylko w pierwszym kółku)
    Pass,               // Plaża
    Game,               // Szukamy partii
    Gf,        // Sforsowani do partii
    Premium,    // Szlem lub szlemik
    MinLoss,       // Minimalizacja straty przez wpadkę
    Penalty,     // Granie pod wpadkę przeciwników (kontra)
}