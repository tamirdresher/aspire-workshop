export interface CatalogItem {
  id: string;
  name: string;
  description: string;
  price: number;
  imageUrl: string;
  availableStock: number;
  category: string;
  brand: string;
}

export interface BasketItem {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  pictureUrl: string;
}

export interface CustomerBasket {
  buyerId: string;
  items: BasketItem[];
}

export interface Order {
  id?: string;
  buyerId: string;
  orderDate?: Date;
  status?: string;
  items: OrderItem[];
  total: number;
  street: string;
  city: string;
  state: string;
  country: string;
  zipCode: string;
}

export interface OrderItem {
  productId: string;
  productName: string;
  unitPrice: number;
  units: number;
  pictureUrl: string;
}

export interface ChatMessage {
  userId: string;
  message: string;
}

export interface ChatResponse {
  message: string;
}
