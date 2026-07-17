// go-api is a tiny HTTP service used to demonstrate hosting a Go application
// from a .NET Aspire AppHost via the Aspire.Hosting.Go integration.
package main

import (
	"encoding/json"
	"log"
	"net/http"
	"os"
	"time"
)

// greeting is the JSON payload returned by the /api/hello endpoint.
type greeting struct {
	Message string    `json:"message"`
	From    string    `json:"from"`
	AtUtc   time.Time `json:"atUtc"`
}

func main() {
	mux := http.NewServeMux()

	mux.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		_, _ = w.Write([]byte("OK"))
	})

	mux.HandleFunc("/api/hello", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		_ = json.NewEncoder(w).Encode(greeting{
			Message: "Hello from the Go service!",
			From:    "go-api",
			AtUtc:   time.Now().UTC(),
		})
	})

	// Aspire assigns the port via the PORT environment variable when the
	// resource is configured with WithHttpEndpoint(env: "PORT"). Fall back to
	// 8080 so the service can still run standalone (e.g. `go run .`).
	port := os.Getenv("PORT")
	if port == "" {
		port = "8080"
	}

	addr := ":" + port
	log.Printf("go-api listening on %s", addr)
	if err := http.ListenAndServe(addr, mux); err != nil {
		log.Fatal(err)
	}
}
