import nextConfig from "eslint-config-next";

const eslintConfig = [
  {
    ignores: [".next/*", "node_modules/*", "dist/*", "coverage/*", "out/*"]
  },
  ...nextConfig,
  {
    rules: {
      "@typescript-eslint/no-explicit-any": "off",
      "@typescript-eslint/no-unused-vars": "off",
      "@typescript-eslint/no-unsafe-function-type": "off",
      "@typescript-eslint/no-empty-object-type": "off",
      "prefer-const": "warn",
      "react-hooks/exhaustive-deps": "off",
      "react/no-unescaped-entities": "off",
      "@next/next/no-img-element": "off",
      "react-hooks/set-state-in-effect": "off",
      "react-hooks/immutability": "off"
    }
  }
];

export default eslintConfig;
