import axios from "axios";

const axiosHttpRequest = axios.create({
  baseURL: "http://localhost:3666/",
  headers: {
    "Content-Type": "application/json",
  },
});
export default axiosHttpRequest;
