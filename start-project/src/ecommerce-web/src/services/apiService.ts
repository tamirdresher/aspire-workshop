import type { CatalogItem, CustomerBasket, Order, ChatMessage, ChatResponse } from '../types';

const API_BASE = {
  catalog: import.meta.env.VITE_CATALOG_API || 'https://localhost:7001',
  basket: import.meta.env.VITE_BASKET_API || 'https://localhost:7002',
  ordering: import.meta.env.VITE_ORDERING_API || 'https://localhost:7003',
  aiAssistant: import.meta.env.VITE_AI_ASSISTANT_API || 'https://localhost:7004',
};

// Catalog Service
export const catalogService = {
  async getProducts(): Promise<CatalogItem[]> {
    const response = await fetch(`${API_BASE.catalog}/api/catalog`);
    if (!response.ok) throw new Error('Failed to fetch products');
    return response.json();
  },

  async getProduct(id: string): Promise<CatalogItem> {
    const response = await fetch(`${API_BASE.catalog}/api/catalog/${id}`);
    if (!response.ok) throw new Error('Failed to fetch product');
    return response.json();
  },

  async getProductsByCategory(category: string): Promise<CatalogItem[]> {
    const response = await fetch(`${API_BASE.catalog}/api/catalog/category/${category}`);
    if (!response.ok) throw new Error('Failed to fetch products by category');
    return response.json();
  },
};

// Basket Service
export const basketService = {
  async getBasket(buyerId: string): Promise<CustomerBasket> {
    const response = await fetch(`${API_BASE.basket}/api/basket/${buyerId}`);
    if (!response.ok) throw new Error('Failed to fetch basket');
    return response.json();
  },

  async updateBasket(basket: CustomerBasket): Promise<CustomerBasket> {
    const response = await fetch(`${API_BASE.basket}/api/basket`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(basket),
    });
    if (!response.ok) throw new Error('Failed to update basket');
    return response.json();
  },

  async checkout(buyerId: string, shippingAddress: string): Promise<void> {
    const response = await fetch(
      `${API_BASE.basket}/api/basket/${buyerId}/checkout?shippingAddress=${encodeURIComponent(shippingAddress)}`,
      { method: 'POST' }
    );
    if (!response.ok) throw new Error('Failed to checkout');
  },
};

// Ordering Service
export const orderingService = {
  async getOrders(buyerId?: string): Promise<Order[]> {
    const url = buyerId 
      ? `${API_BASE.ordering}/api/orders?buyerId=${buyerId}`
      : `${API_BASE.ordering}/api/orders`;
    const response = await fetch(url);
    if (!response.ok) throw new Error('Failed to fetch orders');
    return response.json();
  },

  async getOrder(id: string): Promise<Order> {
    const response = await fetch(`${API_BASE.ordering}/api/orders/${id}`);
    if (!response.ok) throw new Error('Failed to fetch order');
    return response.json();
  },

  async createOrder(order: Order): Promise<Order> {
    const response = await fetch(`${API_BASE.ordering}/api/orders`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(order),
    });
    if (!response.ok) throw new Error('Failed to create order');
    return response.json();
  },
};

// AI Assistant Service
export const aiAssistantService = {
  async sendMessage(chatMessage: ChatMessage): Promise<ChatResponse> {
    const response = await fetch(`${API_BASE.aiAssistant}/api/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(chatMessage),
    });
    if (!response.ok) throw new Error('Failed to send message');
    return response.json();
  },

  async clearConversation(userId: string): Promise<void> {
    const response = await fetch(`${API_BASE.aiAssistant}/api/chat/${userId}`, {
      method: 'DELETE',
    });
    if (!response.ok) throw new Error('Failed to clear conversation');
  },
};
