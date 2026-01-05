import { useState, useEffect } from 'react'
import './App.css'

const API_BASE_URL = 'https://localhost:7032'

function App() {
  const [activeTab, setActiveTab] = useState('books')
  const [books, setBooks] = useState([])
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  
  // New book form state
  const [newBook, setNewBook] = useState({
    title: '',
    author: '',
    price: '',
    stock: '',
    imageUrl: ''
  })

  useEffect(() => {
    if (activeTab === 'books') {
      fetchBooks()
    } else if (activeTab === 'orders') {
      fetchOrders()
    }
  }, [activeTab])

  const fetchBooks = async () => {
    setLoading(true)
    setError(null)
    try {
      const response = await fetch(`${API_BASE_URL}/books`)
      if (!response.ok) throw new Error('Failed to fetch books')
      const data = await response.json()
      setBooks(data)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  const fetchOrders = async () => {
    setLoading(true)
    setError(null)
    try {
      const response = await fetch(`${API_BASE_URL}/orders`)
      if (!response.ok) throw new Error('Failed to fetch orders')
      const data = await response.json()
      setOrders(data)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  const handleAddBook = async (e) => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    
    try {
      const bookData = {
        ...newBook,
        price: parseFloat(newBook.price),
        stock: parseInt(newBook.stock),
        imageUrl: newBook.imageUrl || 'https://picsum.photos/200/300?text=Book+Cover'
      }
      
      const response = await fetch(`${API_BASE_URL}/books`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(bookData)
      })
      
      if (!response.ok) throw new Error('Failed to add book')
      
      // Reset form
      setNewBook({ title: '', author: '', price: '', stock: '', imageUrl: '' })
      
      // Refresh books list
      await fetchBooks()
      
      alert('Book added successfully! The AI worker will generate a description shortly.')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  const handleDeleteBook = async (bookId) => {
    if (!confirm('Are you sure you want to delete this book?')) return
    
    setLoading(true)
    setError(null)
    
    try {
      const response = await fetch(`${API_BASE_URL}/books/${bookId}`, {
        method: 'DELETE'
      })
      
      if (!response.ok) throw new Error('Failed to delete book')
      
      await fetchBooks()
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="admin-container">
      <header className="admin-header">
        <h1>📚 Bookstore Admin Panel</h1>
        <p>Manage your bookstore inventory and orders</p>
      </header>

      <nav className="admin-nav">
        <button
          className={activeTab === 'books' ? 'active' : ''}
          onClick={() => setActiveTab('books')}
        >
          📖 Books
        </button>
        <button
          className={activeTab === 'add-book' ? 'active' : ''}
          onClick={() => setActiveTab('add-book')}
        >
          ➕ Add Book
        </button>
        <button
          className={activeTab === 'orders' ? 'active' : ''}
          onClick={() => setActiveTab('orders')}
        >
          🛒 Orders
        </button>
      </nav>

      <main className="admin-content">
        {error && (
          <div className="error-message">
            ⚠️ Error: {error}
          </div>
        )}

        {activeTab === 'books' && (
          <div className="books-section">
            <h2>Book Inventory</h2>
            {loading ? (
              <div className="loading">Loading books...</div>
            ) : (
              <div className="books-table">
                <table>
                  <thead>
                    <tr>
                      <th>Image</th>
                      <th>Title</th>
                      <th>Author</th>
                      <th>Price</th>
                      <th>Stock</th>
                      <th>Description</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {books.map(book => (
                      <tr key={book.id}>
                        <td>
                          <img src={book.imageUrl} alt={book.title} className="book-thumbnail" />
                        </td>
                        <td>{book.title}</td>
                        <td>{book.author}</td>
                        <td>${book.price.toFixed(2)}</td>
                        <td>{book.stock}</td>
                        <td className="description-cell">{book.description}</td>
                        <td>
                          <button 
                            className="delete-btn"
                            onClick={() => handleDeleteBook(book.id)}
                          >
                            🗑️
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}

        {activeTab === 'add-book' && (
          <div className="add-book-section">
            <h2>Add New Book</h2>
            <form onSubmit={handleAddBook} className="book-form">
              <div className="form-group">
                <label>Title *</label>
                <input
                  type="text"
                  value={newBook.title}
                  onChange={(e) => setNewBook({ ...newBook, title: e.target.value })}
                  required
                />
              </div>
              
              <div className="form-group">
                <label>Author *</label>
                <input
                  type="text"
                  value={newBook.author}
                  onChange={(e) => setNewBook({ ...newBook, author: e.target.value })}
                  required
                />
              </div>
              
              <div className="form-group">
                <label>Price *</label>
                <input
                  type="number"
                  step="0.01"
                  value={newBook.price}
                  onChange={(e) => setNewBook({ ...newBook, price: e.target.value })}
                  required
                />
              </div>
              
              <div className="form-group">
                <label>Stock *</label>
                <input
                  type="number"
                  value={newBook.stock}
                  onChange={(e) => setNewBook({ ...newBook, stock: e.target.value })}
                  required
                />
              </div>
              
              <div className="form-group">
                <label>Image URL (optional)</label>
                <input
                  type="url"
                  value={newBook.imageUrl}
                  onChange={(e) => setNewBook({ ...newBook, imageUrl: e.target.value })}
                  placeholder="https://example.com/image.jpg"
                />
              </div>
              
              <button type="submit" className="submit-btn" disabled={loading}>
                {loading ? 'Adding...' : 'Add Book'}
              </button>
              
              <p className="note">
                📝 Note: The description will be generated automatically by our AI worker service.
              </p>
            </form>
          </div>
        )}

        {activeTab === 'orders' && (
          <div className="orders-section">
            <h2>Customer Orders</h2>
            {loading ? (
              <div className="loading">Loading orders...</div>
            ) : orders.length === 0 ? (
              <p className="no-data">No orders yet.</p>
            ) : (
              <div className="orders-list">
                {orders.map(order => (
                  <div key={order.id} className="order-card">
                    <div className="order-header">
                      <h3>Order #{order.id.substring(0, 8)}</h3>
                      <span className="order-status">{order.status}</span>
                    </div>
                    <div className="order-details">
                      <p><strong>Customer:</strong> {order.customerName}</p>
                      <p><strong>Email:</strong> {order.customerEmail}</p>
                      <p><strong>Date:</strong> {new Date(order.orderDate).toLocaleString()}</p>
                      <p><strong>Total:</strong> ${order.totalAmount.toFixed(2)}</p>
                    </div>
                    <div className="order-items">
                      <h4>Items:</h4>
                      <ul>
                        {order.items.map((item, idx) => (
                          <li key={idx}>
                            {item.book?.title} x {item.quantity} - ${item.totalPrice.toFixed(2)}
                          </li>
                        ))}
                      </ul>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  )
}

export default App
