#ifndef MATRIX_H
#define MATRIX_H

#include "pugixml.hpp"

class Matrix {
private:
	pugi::xml_node root;
	double a, b, c, d, tx, ty;
public:
	Matrix(pugi::xml_node& matrixNode) noexcept;
	Matrix(const pugi::xml_node& matrixNode) noexcept;
	~Matrix() noexcept;
	Matrix(const Matrix& matrix) noexcept;
	double getA() const noexcept;
	void setA(double a) noexcept;
	double getB() const noexcept;
	void setB(double b) noexcept;
	double getC() const noexcept;
	void setC(double c) noexcept;
	double getD() const noexcept;
	void setD(double d) noexcept;
	double getTx() const noexcept;
	void setTx(double tx) noexcept;
	double getTy() const noexcept;
	void setTy(double ty) noexcept;
	pugi::xml_node& getRoot() noexcept;
	const pugi::xml_node& getRoot() const noexcept;
};

#endif // MATRIX_H