import { useState, useEffect } from 'react';
import { catalogService } from './services/apiService';
import type { CatalogItem } from './types';
import './App.css';

function App() {
  const [products, setProducts] = useState<CatalogItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadProducts();
  }, []);

  const loadProducts = async () => {
    try {
      setLoading(true);
      const data = await catalogService.getProducts();
      setProducts(data);
      setError(null);
    } catch (err) {
      setError('Failed to load products. Make sure the Catalog API is running at https://localhost:7001');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="App">
      <header className="App-header">
        <h1>🛒 ECommerce Store</h1>
        <p>Modern Microservices E-commerce Platform</p>
      </header>

      <main className="App-main">
        <section className="hero">
          <h2>Welcome to the Aspire Workshop E-commerce Demo</h2>
          <p>
            This is a sophisticated brownfield application with microservices architecture.
            Browse products, manage your cart, and experience AI-powered assistance!
          </p>
          <div className="services-grid">
            <div className="service-card">
              <h3>📦 Catalog Service</h3>
              <p>Product management with Cosmos DB</p>
              <span className="port">Port: 7001</span>
            </div>
            <div className="service-card">
              <h3>🛍️ Basket Service</h3>
              <p>Shopping cart with Azure Queue</p>
              <span className="port">Port: 7002</span>
            </div>
            <div className="service-card">
              <h3>📋 Ordering Service</h3>
              <p>Order processing & notifications</p>
              <span className="port">Port: 7003</span>
            </div>
            <div className="service-card">
              <h3>🤖 AI Assistant</h3>
              <p>Powered by Azure OpenAI</p>
              <span className="port">Port: 7004</span>
            </div>
          </div>
        </section>

        <section className="products-section">
          <h2>Product Catalog</h2>
          
          {loading && <p>Loading products...</p>}
          
          {error && (
            <div className="error-message">
              <p>{error}</p>
              <button onClick={loadProducts}>Retry</button>
            </div>
          )}
          
          {!loading && !error && products.length === 0 && (
            <div className="info-message">
              <p>No products available yet.</p>
              <p>The Catalog service is running but has no data. Add products via the API or seed the database.</p>
            </div>
          )}
          
          {!loading && !error && products.length > 0 && (
            <div className="products-grid">
              {products.map((product) => (
                <div key={product.id} className="product-card">
                  <div className="product-image">
                    {product.imageUrl ? (
                      <img src={product.imageUrl} alt={product.name} />
                    ) : (
                      <div className="placeholder-image">📦</div>
                    )}
                  </div>
                  <div className="product-info">
                    <h3>{product.name}</h3>
                    <p className="product-description">{product.description}</p>
                    <div className="product-meta">
                      <span className="category">{product.category}</span>
                      <span className="brand">{product.brand}</span>
                    </div>
                    <div className="product-footer">
                      <span className="price">${product.price.toFixed(2)}</span>
                      <span className="stock">
                        {product.availableStock > 0 
                          ? `${product.availableStock} in stock` 
                          : 'Out of stock'}
                      </span>
                    </div>
                    <button 
                      className="add-to-cart-btn"
                      disabled={product.availableStock === 0}
                    >
                      Add to Cart
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>

        <section className="info-section">
          <h2>🚀 Next Steps</h2>
          <p>
            This is the <strong>brownfield</strong> version of the application. During the workshop, you'll:
          </p>
          <ol>
            <li>Add .NET Aspire orchestration to manage all microservices</li>
            <li>Configure Azure resource integrations (Cosmos DB, Queue Storage, OpenAI)</li>
            <li>Use the Aspire Dashboard for centralized observability</li>
            <li>Deploy to Azure Container Apps with a single command</li>
          </ol>
        </section>
      </main>

      <footer className="App-footer">
        <p>Built with React + TypeScript • .NET 9 • Azure Services</p>
        <p>Part of the .NET Aspire Workshop</p>
      </footer>
    </div>
  );
}

export default App;
