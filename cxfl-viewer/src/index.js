import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './main.js';
import reportWebVitals from './reportWebVitals';
import './globalStyles.css';

const root = ReactDOM.createRoot(document.getElementById('root'));

root.render(
  <React.StrictMode>
    <App/>
  </React.StrictMode>
);

reportWebVitals();